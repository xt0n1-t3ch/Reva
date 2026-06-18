using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reva.App.Composition;

namespace Reva.App.Onboarding;

public sealed class TourStateStore : ITourStateStore
{
    private const string StateFileName = "onboarding.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly string _statePath;

    public TourStateStore()
        : this(Path.Combine(AppDataPaths.Root, StateFileName))
    {
    }

    public TourStateStore(string statePath)
    {
        _statePath = statePath;
    }

    public bool HasSeenTour()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return false;
            }

            var json = File.ReadAllText(_statePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var state = JsonSerializer.Deserialize<TourState>(json, SerializerOptions);
            return state?.HasSeenTour ?? false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    public void MarkTourSeen()
    {
        try
        {
            var directory = Path.GetDirectoryName(_statePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(new TourState(true), SerializerOptions);
            File.WriteAllText(_statePath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed record TourState(
        [property: JsonPropertyName("hasSeenTour")] bool HasSeenTour);
}
