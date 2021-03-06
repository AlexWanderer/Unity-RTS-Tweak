﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public struct AnalyticData {
	public float timeStamp;
	public string readableTimeStamp;
	public string input;

	public AnalyticData(string data) {
		this.timeStamp = Analytics.Instance.sessionTimer;
		this.input = data;

		int minutes = ((int) Mathf.Floor(Analytics.Instance.sessionTimer)) / 60;
		int seconds = ((int) Mathf.Floor(Analytics.Instance.sessionTimer)) % 60;
		int normalized = ((int) Analytics.Instance.sessionTimer);
		float mantissa = Analytics.Instance.sessionTimer - (float) normalized;
		mantissa *= 1000f;
		normalized = (int) mantissa;
		this.readableTimeStamp = minutes.ToString() + "m " + seconds.ToString() + "." + normalized.ToString() + "s";
	}
}

public class Analytics : MonoBehaviour {
	public static Analytics Instance;

	public float sessionTimer;
	public bool sessionTimerTickFlag;
	public List<AnalyticData> sessionTimerData;
	public bool serverSessionReadyFlag;

	private NetworkView analyticNetworkView;
	private bool showTimerFlag;
	private string timeString;

	public void Start() {
		Analytics.Instance = this;
		this.sessionTimer = 0f;
		this.sessionTimerTickFlag = false;
		this.sessionTimerData = new List<AnalyticData>();
		this.analyticNetworkView = this.GetComponent<NetworkView>();
		this.showTimerFlag = false;
	}

	public void OnPlayerConnected(NetworkPlayer player) {
		//Debug.LogWarning("Player has connected. Timer analytic stuff set to start.");
		Analytics.Instance.AddEvent("Game session begins.");
		if (this.analyticNetworkView != null && this.analyticNetworkView.isMine) {
			this.analyticNetworkView.RPC("RPC_StartTimer", RPCMode.AllBuffered);
		}
	}

	public void Update() {
		if (this.sessionTimerTickFlag) {
			this.sessionTimer += Time.deltaTime;
		}
	}

	public void OnGUI() {
		if (this.showTimerFlag) {
			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.red;
			style.fontStyle = FontStyle.Bold;
			style.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect((Screen.width - 80) / 2f, (Screen.height + 25) / 2f, 135f, 40f), "Total Gameplay Time: " + this.timeString + "s.", style);
		}
	}

	public void StopTimer() {
		//Debug.LogWarning("Session Timer has stopped.");
		this.sessionTimerTickFlag = false;
		this.AddEvent("Session Timer has stopped.");

		int minutes = ((int) Mathf.Floor(this.sessionTimer)) / 60;
		int seconds = ((int) Mathf.Floor(this.sessionTimer)) % 60;
		int normalized = ((int) this.sessionTimer);
		float mantissa = this.sessionTimer - (float) normalized;
		mantissa *= 1000f;
		normalized = (int) mantissa;
		this.timeString = minutes.ToString() + ":" + seconds.ToString() + "." + normalized.ToString();
	}

	public void StartTimer() {
		//Debug.LogWarning("Session Timer has started.");
		this.sessionTimerTickFlag = true;
	}

	public bool isTimerStarted() {
		return this.sessionTimerTickFlag;
	}

	public void CreateTimerLogData() {
		//if (!(Application.platform == RuntimePlatform.OSXWebPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)) {
		//	//System.IO.File.WriteAllText("analytics.txt", "Total Gameplay Time: " + this.sessionTimer.ToString() + "s.");
		//}
		if (!this.showTimerFlag) {
			this.showTimerFlag = true;
			StringBuilder builder = new StringBuilder();
			builder.AppendLine();
			foreach (AnalyticData data in this.sessionTimerData) {
				builder.AppendLine(data.readableTimeStamp + " : " + data.input);
			}
			throw new System.Exception(builder.ToString());
		}
	}

	public void AddEvent(string data) {
		AnalyticData log = new AnalyticData(data);
		this.sessionTimerData.Add(log);
		Debug.Log(log.readableTimeStamp + " : " + log.input);
	}

	[RPC]
	public void RPC_StartTimer() {
		this.StartTimer();
	}
}
