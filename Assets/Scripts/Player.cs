using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool isPlayer = false;
    public static Player localPlayer { get; set; }

    public GameObject boat;

    public Color color;
    public float colorIntensity;

    private void UpdateMaterial(Material mat, float alpha)
    {
        var color = mat.color;
        color.a = alpha;
        mat.color = color;
    }

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 velocity;

    public int maxRotations = 7;

    [Header("Paddle Left")]
    public GameObject paddleLeft;
    public Quaternion paddleLeftStart;
    public Quaternion paddleLeftEnd;

    [Header("Paddle Right")]
    public GameObject paddleRight;
    public Quaternion paddleRightStart;
    public Quaternion paddleRightEnd;

    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
        endPosition = transform.position;

        paddleLeft.transform.localRotation = paddleLeftStart;
        paddleRight.transform.localRotation = paddleRightStart;

        rotationPercentageIncrement = 1 / (float)maxRotations;
    }

    public void UpdateColor()
    {
        var mesh = boat.GetComponent<MeshRenderer>();

        // Update the color of the player to match the profile color
        mesh.materials[1].color = color;
        mesh.materials[1].SetVector("_EmissionColor", color * colorIntensity);

        // If this is not the main player we should make it appear like a ghost
        if (!isPlayer)
        {
            UpdateMaterial(mesh.materials[0], 0.25f);
            UpdateMaterial(mesh.materials[1], 0.6f);
            UpdateMaterial(mesh.materials[2], 0.25f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPlayer)
        {
            return;
        }
        // When our player enters the finish line trigger
        if (other.CompareTag("FinishLine"))
        {
            GameManager.instance.FinishGame();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayer)
        {
            // This is debug code to help test changes without using the rowing machine. Scroll to move the player
            if (Input.mouseScrollDelta.y > 0)
            {
                Debug.Log("Forward");
                ProcessInput("A");
                ProcessInput("B");
            }
            else if (Input.mouseScrollDelta.y < 0)
            {
                Debug.Log("Reverse");
                ProcessInput("B");
                ProcessInput("A");
            }
        }

        if (!GameManager.instance.isGameStarted)
        {
            return;
        }

        // Smoothly move our player based on how many rotations of machine
        transform.position = Vector3.SmoothDamp(transform.position, endPosition, ref velocity, speed);

        paddleLeft.transform.localRotation = Quaternion.Lerp(paddleLeftStart, paddleLeftEnd, rotationPercentage);
        paddleRight.transform.localRotation = Quaternion.Lerp(paddleRightStart, paddleRightEnd, rotationPercentage);
    }

    private int strokes = 0;
    private float distance = 0f;

    private float rotationPercentage;
    private float rotationPercentageIncrement;

    private List<float> delays = new List<float>();

    private float speed = 4;

    public void OnRotation()
    {
        distance += GameManager.instance.rotationDistance;

        // Set the target position for the player based on each rotation
        endPosition.z = startPosition.z + distance * GameManager.instance.rotationMultiplier;

        // Set the paddle rotation based on how many rotations there are
        rotationPercentage = rotationPercentageIncrement * (float)rotations;

        // Calculate the speed of the boat based on time between rotations on average
        // This is not perfect or scientific in any way. Its mostly about feeling right
        // Adjust speedMultiplayer property on GameManager to fine tune
        if (delays.Count() > 5)
        {
            var average = delays.TakeLast(5).Average();
            if (average == 0)
            {
                average = 1;
            }
            speed = average / GameManager.instance.speedMultiplayer;
        }
        else
        {
            Debug.LogWarning("No past inputs to average");
        }

        if (isPlayer)
        {
            GameManager.instance.UpdateDistance(distance);
        }
    }

    public void OnStroke(int rotations)
    {
        if (rotations > 0)
        {
            // This happens when we start a new stroke but we thought we werent done returning from stroke
            Debug.LogWarning("OnStroke -- BAD " + rotations);
        }
        else
        {
            Debug.Log("OnStroke");
        }

        strokes++;
        if (isPlayer)
        {
            GameManager.instance.UpdateStrokes(strokes);
        }
    }

    private string firstButton = "";
    private string lastButton = "";

    private bool forward = true;
    private int rotations = 1;

    public void ProcessInput(string button, bool skipRecord = false)
    {
        var ticks = DateTime.Now.Ticks;
        float delaySec = 0;

        if (lastRecordedTime > 0)
        {
            // Calculate the delay since the last input so we can replay it later accurately
            var span = new TimeSpan(ticks - lastRecordedTime);
            delaySec = (float)span.TotalSeconds;
        }

        lastRecordedTime = ticks;

        if (!skipRecord)
        {
            recordedInput.Add(new InputData() { Button = button, Delay = delaySec });
        }

        if (firstButton == "")
        {
            if (isPlayer)
            {
                // This is the first input we have received, this means we know were heading in forward direction and can start the game
                GameManager.instance.StartGame();
            }

            OnRotation();
            Debug.Log("First: " + button);
            firstButton = button;
            lastButton = button;
            return;
        }

        var isFirst = button == firstButton;

        if (isFirst)
        {
            delays.Add(delaySec);
        }

        if (lastButton == button)
        {
            forward = !forward;
            if (forward)
            {
                Debug.Log("Forward (Rot=" + rotations + ")");
                if (rotations > 0)
                {
                    // Something went wrong but the stroke was finished - maybe add timeout on last reverse rotation to register stroke instead of on next stroke start
                    OnStroke(rotations);
                    rotations = 0;
                }
            }
            else
            {
                Debug.Log("Reverse");
            }
        }

        if (forward)
        {
            rotations++;
            if (isFirst)
            {
                OnRotation();
            }
        }
        else
        {
            rotations--;
        }

        if (isPlayer)
        {
            GameManager.instance.UpdateRotations(rotations);
        }

        rotationPercentage = rotationPercentageIncrement * (float)rotations;

        if (rotations == 0)
        {
            OnStroke(rotations);
        }

        lastButton = button;
    }

    #region Replay
    // This code allows us to replay all of our input accurately when we play against our best times later
    internal List<InputData> recordedInput = new List<InputData>();
    private long lastRecordedTime;
    public void Replay()
    {
        if (recordedInput.Count > 0)
        {
            replayCoroutine = ReplayRecordedInput();
            StartCoroutine(replayCoroutine);
        }
    }

    private IEnumerator replayCoroutine;

    private IEnumerator ReplayRecordedInput()
    {
        for (int i = 0; i < recordedInput.Count; i++)
        {
            var next = recordedInput[i];

            yield return new WaitForSeconds(next.Delay);

            ProcessInput(next.Button, true);
        }
    }
    #endregion
}
