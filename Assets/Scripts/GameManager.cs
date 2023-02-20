using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    [Header("Player")]
    public GameObject playerPrefab;
    public float playerSpawnOffset = 8;
    public Cinemachine.CinemachineVirtualCamera virtualCamera;

    [Header("Race")]
    public float speedMultiplayer = 1f;
    public float finishDistance = 1;
    public GameObject finishLine;
    public float rotationDistance = 0.1f;
    public float rotationMultiplier = 100f;

    [Header("UI")]
    public GameObject statsRow;
    public TMPro.TextMeshProUGUI strokesLabel;
    public TMPro.TextMeshProUGUI rpmLabel;
    public TMPro.TextMeshProUGUI speedLabel;
    public TMPro.TextMeshProUGUI timeLabel;
    public TMPro.TextMeshProUGUI distanceLabel;
    public GameObject finishPanel;

    public GameObject logos;

    private float timeStart = 0;
    internal bool isGameStarted = false;

    private List<Player> otherPlayers = new List<Player>();

    // Start is called before the first frame update
    void Start()
    {
        if (ProfileManager.instance == null)
        {
            MainMenu();
            return;
        }
        instance = this;

        // Set defaults for UI
        finishPanel.SetActive(false);
        statsRow.SetActive(false);
        strokesLabel.text = "0";
        rpmLabel.text = "0";
        speedLabel.text = "0";
        timeLabel.text = "00:00";

        // Move finish line to correct spot - race distance can be adjusted at runtime
        var finPos = finishLine.transform.position;
        finishLine.transform.position = new Vector3(finPos.x, finPos.y, finishDistance * rotationMultiplier);

        // Spawn player at starting point which is 0, 0, 0
        var playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        var player = playerObject.GetComponent<Player>();
        // This will be our main player so other parts of the game know
        player.isPlayer = true;
        Player.localPlayer = player;

        // Tell camera to follow our player
        virtualCamera.Follow = playerObject.transform;
        virtualCamera.LookAt = playerObject.transform;

        // Set the player color based on what we chose on first screen
        var playerColor = Color.red;
        if (ColorUtility.TryParseHtmlString(ProfileManager.instance.currentProfile.Color, out playerColor))
        {
            player.color = playerColor;
            player.UpdateColor();
        }

        if (ProfileManager.instance.enableGhosts)
        {
            LoadGhosts();
        }
    }

    private void LoadGhosts()
    {
        foreach(var profile in ProfileManager.instance.profiles)
        {
            LoadBestFromProfile(profile);
        }
    }

    private void LoadBestFromProfile(Profile profile)
    {
        if (!profile.Sessions.Any())
        {
            return;
        }
        var bestSession = profile.Sessions.OrderBy(p => p.Duration).FirstOrDefault();

        // Spawn ghost based on offset
        var playerObject = Instantiate(playerPrefab, new Vector3(-playerSpawnOffset, 0, 0), Quaternion.identity);
        var player = playerObject.GetComponent<Player>();
        // This is not our main player so it shouldnt get input from Arduino
        player.isPlayer = false;
        // Load past input data to replay properly
        player.recordedInput = bestSession.Input;

        var playerColor = Color.red;
        if (ColorUtility.TryParseHtmlString(profile.Color, out playerColor))
        {
            player.color = playerColor;
            player.UpdateColor();
        }

        otherPlayers.Add(player);
    }

    // Update is called once per frame
    void Update()
    {
        // Update time elapsed
        UpdateGameTime();
    }

    private void UpdateGameTime()
    {
        if (!isGameStarted) { return; }
        var t = Time.time - timeStart;
        var span = TimeSpan.FromSeconds((double)(new decimal(t)));
        if (span.TotalHours >= 1)
        {
            Debug.Log(span.TotalHours);
            timeLabel.text = span.ToString(@"hh\:mm\:ss");
        }
        else
        {
            timeLabel.text = span.ToString(@"mm\:ss");
        }
    }

    public void UpdateSpeed(float speed)
    {
        speedLabel.text = speed.ToString();
    }

    public void UpdateStrokes(int strokes)
    {
        strokesLabel.text = strokes.ToString();
    }

    public void UpdateDistance(float distance)
    {
        if (distance >= finishDistance)
        {
            distanceLabel.text = finishDistance.ToString();
            return;
        }
        distanceLabel.text = Math.Round(distance, 2).ToString();
    }

    public void FinishGame()
    {
        finishPanel.SetActive(true);
        isGameStarted = false;

        var t = Time.time - timeStart;
        var span = TimeSpan.FromSeconds((double)(new decimal(t)));

        var currentSession = new GameSessionData()
        {
            Distance = finishDistance,
            Date = DateTime.Now,
            Duration = span.TotalMilliseconds,
            Input = Player.localPlayer.recordedInput,
        };

        ProfileManager.instance.currentProfile.Sessions.Add(currentSession);
        ProfileManager.instance.SaveProfiles();
    }

    public void StartGame()
    {
        timeStart = Time.time;
        isGameStarted = true;

        StartGhosts();
    }

    private void StartGhosts()
    {
        foreach(var p in otherPlayers)
        {
            p.Replay();
        }
    }

    public void Ready()
    {
        // The Arduino is connected and we can start taking input
        statsRow.SetActive(true);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
