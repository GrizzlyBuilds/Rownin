using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager instance { get; private set; }
    public List<Color> colors = new List<Color>();

    [NonSerialized]
    public bool enableGhosts = true;

    public void UpdateGhostsPreference(bool newValue)
    {
        enableGhosts = newValue;
        Debug.Log("Ghosts Enabled: " + enableGhosts);
    }

    public void ResetProfiles()
    {
        Debug.LogWarning("Reset profiles");
        profiles = new List<Profile>();
        SaveProfiles();
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);

        // This is a hacky way to map colors to profile names. We dont have access to a on screen keyboard so we cant type a profile name. We could make our own keyboard but thats too much work for this demo
        colorMappings.Add("orange", colors[0]);
        colorMappings.Add("green", colors[1]);
        colorMappings.Add("blue", colors[2]);
        colorMappings.Add("pink", colors[3]);

        // Load local profile data from past sessions
        LoadProfiles();
    }

    public List<Profile> profiles = new List<Profile>();
    [NonSerialized]
    public Profile currentProfile;

    private const string PROFILE_FILANME = "profiles.json";
    public void LoadProfiles()
    {
        if (!File.Exists(PROFILE_FILANME))
        {
            return;
        }
        string json = File.ReadAllText(PROFILE_FILANME);
        profiles = JsonConvert.DeserializeObject<List<Profile>>(json);
        Debug.Log("Loaded profiles: " + profiles.Count);
    }

    public void SaveProfiles()
    {
        string json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
        File.WriteAllText(PROFILE_FILANME, json);
        Debug.LogWarning("Saved profiles");
    }

    public void SelectProfile(string name)
    {
        var profile = profiles.FirstOrDefault(p => p.Name == name);
        if (profile == null)
        {
            // This profile has not been used before so we need to create it
            profile = new Profile()
            {
                Name = name,
                Color = "#" + ColorUtility.ToHtmlStringRGB(colorMappings[name]),
                Sessions = new List<GameSessionData>(),
            };
            profiles.Add(profile);
        }

        currentProfile = profile;
        Debug.Log("Selected Profile: " + name + " | " + profile.Color);

        SceneManager.LoadScene(1);
    }

    private Dictionary<string, Color> colorMappings = new Dictionary<string, Color>();
}

public class Profile
{
    public string Name { get; set; }
    public string Color { get; set; }
    public List<GameSessionData> Sessions { get; set; }
}


public class GameSessionData
{
    public float Distance { get; set; }
    public DateTime Date { get; set; }
    public double Duration { get; set; }
    public List<InputData> Input { get; set; }
}
public class InputData
{
    public string Button { get; set; }
    public float Delay { get; set; }
}