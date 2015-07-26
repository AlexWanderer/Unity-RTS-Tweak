﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct MergePair {
	public GameObject first;
	public GameObject second;
	public float elapsedTime;
	public Vector3 firstInitialPosition;
	public Vector3 secondInitialPosition;
	public Vector3 firstInitialScale;
	public Vector3 secondInitialScale;
	public Vector3 average {
		get {
			return (this.firstInitialPosition + ((this.secondInitialPosition - this.firstInitialPosition) / 2f));
		}
	}

	public MergePair(GameObject a, GameObject b) {
		this.first = a;
		this.second = b;
		this.elapsedTime = 0f;
		this.firstInitialPosition = a.transform.position;
		this.secondInitialPosition = b.transform.position;
		this.firstInitialScale = a.transform.localScale;
		this.secondInitialScale = b.transform.localScale;

		HealthBar health = a.GetComponent<HealthBar>();
		if (health != null) {
			health.currentHealth *= 2;
			health.maxHealth *= 2;
			health.healthPercentage = (float) health.currentHealth / (float) health.maxHealth;
		}

		health = b.GetComponent<HealthBar>();
		if (health != null) {
			health.currentHealth *= 2;
			health.maxHealth *= 2;
			health.healthPercentage = (float) health.currentHealth / (float) health.maxHealth;
		}

		Attackable attack = a.GetComponent<Attackable>();
		if (attack != null) {
			attack.attackPower *= 2;
		}
		attack = b.GetComponent<Attackable>();
		if (attack != null) {
			attack.attackPower *= 2;
		}

		Level level = a.GetComponent<Level>();
		if (level != null) {
			level.IncrementLevel();
		}
		level = b.GetComponent<Level>();
		if (level != null) {
			level.IncrementLevel();
		}

		Selectable select = a.GetComponent<Selectable>();
		if (select != null) {
			select.isSelected = false;
			select.isBoxedSelected = false;
		}
		select = b.GetComponent<Selectable>();
		if (select != null) {
			select.isSelected = false;
			select.isBoxedSelected = false;
		}

		//Mergeable merge = a.GetComponent<Mergeable>();
		//if (merge != null) {
		//	merge.mergeLevel++;
		//}
		//merge = b.GetComponent<Mergeable>();
		//if (merge != null) {
		//	merge.mergeLevel++;
		//}
	}
};

public class Mergeable : MonoBehaviour {
	private Selectable ownerSelectable;
	private Attackable ownerAttackable;
	private static List<MergePair> pairs = new List<MergePair>();
	private NetworkView playerNetworkView;
	private bool confirmedDestroyed;

	public int mergeLevel;

	[RPC]
	public void RPC_AddPair(NetworkViewID firstViewID, NetworkViewID secondViewID) {
		NetworkView first = NetworkView.Find(firstViewID);
		NetworkView second = NetworkView.Find(secondViewID);
		pairs.Add(new MergePair(first.gameObject, second.gameObject));
	}

	public void Start() {
		this.ownerSelectable = this.GetComponent<Selectable>();
		Mergeable.pairs.Clear();
		this.playerNetworkView = this.GetComponent<NetworkView>();
		this.ownerAttackable = this.GetComponent<Attackable>();
		this.confirmedDestroyed = false;
		//this.mergeLevel = 1;
	}

	public void Update() {
		if (Mergeable.pairs.Count > 0) {
			this.StartCoroutine(CR_Action());
		}
	}

	public void OnGUI() {
		if (Input.GetKeyDown(KeyCode.D) && this.ownerSelectable.isSelected && !(this.ownerAttackable.isReadyToAttack || this.ownerAttackable.isAttacking)) {
			this.ownerSelectable.isSelected = false;
			//this.ownerSelectable.DisableSelection();
			if (this.playerNetworkView != null) {
				foreach (Selectable sel in Selectable.selectedObjects) {
					sel.isSelected = false;
					sel.isBoxedSelected = false;
				}
				int count = Selectable.selectedObjects.Count;
				int countCheck = count;
				int index = 0;
				while (count >= 2 && countCheck >= 2) {
					Selectable firstSelectable = Selectable.selectedObjects[index];
					Selectable secondSelectable = Selectable.selectedObjects[index+1];
					if (firstSelectable != null && secondSelectable != null) {
						Level levelFirst = firstSelectable.gameObject.GetComponent<Level>();
						Level levelSecond = secondSelectable.gameObject.GetComponent<Level>();
						if (levelFirst != null && levelSecond != null && (levelFirst.Compare(levelSecond) == Level.EQUALS)) {
							NetworkView firstNetworkView = firstSelectable.GetComponent<NetworkView>();
							NetworkView secondNetworkView = secondSelectable.GetComponent<NetworkView>();
							this.playerNetworkView.RPC("RPC_AddPair", RPCMode.AllBuffered, firstNetworkView.viewID, secondNetworkView.viewID);
							firstSelectable.DisableSelection();
							secondSelectable.DisableSelection();
							Selectable.selectedObjects.Remove(firstSelectable);
							Selectable.selectedObjects.Remove(secondSelectable);
						}
						else {
							Selectable.selectedObjects.Remove(firstSelectable);
							Selectable.selectedObjects.Remove(secondSelectable);
							Selectable.selectedObjects.Add(firstSelectable);
							Selectable.selectedObjects.Add(secondSelectable);
							countCheck -= 2;
						}
					}
					else {
						index++;
						if (index >= count) {
							break;
						}
						continue;
					}
					count -= 2;
				}
				Selectable.selectedObjects.Clear();
			}
		}
	}

	private IEnumerator CR_Action() {
		for (int i = 0; i < Mergeable.pairs.Count; i++) {
			MoveTo(i);
			Scale(i, 0f, 1f);
			Update(i);

			MergePair pair = Mergeable.pairs[i];
			if (pair.elapsedTime > 1f) {
				if (pair.first != null) {
					Selectable select = pair.first.GetComponent<Selectable>();
					if (select != null) {
						select.EnableSelection();
					}
				}

				bool isDestroyed = false;
				if (pair.second != null) {
					if (pair.second.activeSelf && this.playerNetworkView.isMine) {
						Network.RemoveRPCsInGroup(0);
						Network.Destroy(pair.second);
					
						Divisible div = pair.first.GetComponent<Divisible>();
						if (div != null && div.IsDivisible()) {
							div.SetDivisible(false);
						}
					}

					Mergeable secondMerge = pair.second.GetComponent<Mergeable>();
					if (secondMerge.confirmedDestroyed) {
						isDestroyed = true;
						pair.second = null;
					}
				}

				if (pair.first == null || isDestroyed) {
					Mergeable.pairs.Remove(pair);
				}
			}
			else {
				Selectable select = pair.first.GetComponent<Selectable>();
				if (select != null) {
					select.DisableSelection();
				}
				select = pair.second.GetComponent<Selectable>();
				if (select != null) {
					select.DisableSelection();
				}
			}
			yield return null;
		}
	}

	private void Scale(int i, float multiplierFrom, float multiplierTo) {
		MergePair pair = Mergeable.pairs[i];
		if (pair.first != null && pair.second != null) {
			float value = Mathf.Lerp(multiplierFrom, multiplierTo, pair.elapsedTime);
			pair.first.transform.localScale = pair.firstInitialScale + new Vector3(value, value, value);
			pair.second.transform.localScale = pair.secondInitialScale + new Vector3(value, value, value);
			Mergeable.pairs[i] = pair;
		}
	}

	private void MoveTo(int i) {
		MergePair pair = Mergeable.pairs[i];
		if (pair.first != null && pair.second != null) {
			pair.first.transform.position = Vector3.Lerp(pair.firstInitialPosition, pair.average, pair.elapsedTime);
			pair.second.transform.position = Vector3.Lerp(pair.secondInitialPosition, pair.average, pair.elapsedTime);
			Mergeable.pairs[i] = pair;
		}
	}

	private void Update(int i) {
		MergePair pair = Mergeable.pairs[i];
		pair.elapsedTime += Time.deltaTime / 15f;
		Mergeable.pairs[i] = pair;
	}

	public void OnDestroy() {
		this.confirmedDestroyed = true;
	}
}
