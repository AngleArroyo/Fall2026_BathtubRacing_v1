
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public static class ControlsInfo
{
    public struct ControlEntry
    {
        public string Action;
        public string Keyboard;
        public string Controller;

        public ControlEntry(string action, string keyboard, string controller)
        {
            Action = action;
            Keyboard = keyboard;
            Controller = controller;
        }
    }

    enum Device { Keyboard, Gamepad }

    const string None = "-";

    static readonly Dictionary<string, string> gamepadNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "buttonSouth", "A" },
        { "buttonEast", "B" },
        { "buttonWest", "X" },
        { "buttonNorth", "Y" },
        { "start", "Start" },
        { "select", "Back" },
        { "leftShoulder", "LB" },
        { "rightShoulder", "RB" },
        { "leftTrigger", "LT" },
        { "rightTrigger", "RT" },
        { "leftStickPress", "Left Stick Click" },
        { "rightStickPress", "Right Stick Click" },
        { "leftStick", "Left Stick" },
        { "rightStick", "Right Stick" },
        { "dpad", "D-Pad" },
    };

    static InputActions fallbackActions;

    public static List<ControlEntry> GetRaceControls()
    {
        var actions = InputManager.InputActions;

        if (actions == null)
            actions = fallbackActions ??= new InputActions();

        var move = actions.Player.Move;

        return new List<ControlEntry>
        {
            new("Accelerate", Part(move, "up", Device.Keyboard), Part(move, "up", Device.Gamepad)),
            new("Brake / Reverse", Part(move, "down", Device.Keyboard), Part(move, "down", Device.Gamepad)),
            new("Steer", Steer(move, Device.Keyboard), Steer(move, Device.Gamepad)),
            new("Boost", Simple(actions.Player.Boost, Device.Keyboard), Simple(actions.Player.Boost, Device.Gamepad)),
            new("Drift", Simple(actions.Player.Drift, Device.Keyboard), Simple(actions.Player.Drift, Device.Gamepad)),
            new("Pause", Simple(actions.UI.Menu, Device.Keyboard), Simple(actions.UI.Menu, Device.Gamepad)),
        };
    }

    static string Simple(InputAction action, Device device)
    {
        var names = new List<string>();

        foreach (var binding in action.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite || !IsDevice(binding, device))
                continue;

            AddUnique(names, DisplayName(binding, device));
        }

        return Join(names);
    }

    static string Part(InputAction action, string partName, Device device)
    {
        return Join(PartNames(action, partName, device));
    }

    static string Steer(InputAction action, Device device)
    {
        var lefts = PartNames(action, "left", device);
        var rights = PartNames(action, "right", device);
        var pairs = new List<string>();

        int count = Math.Min(lefts.Count, rights.Count);

        for (int i = 0; i < count; i++)
            AddUnique(pairs, CombinePair(lefts[i], rights[i]));

        return Join(pairs);
    }

    static List<string> PartNames(InputAction action, string partName, Device device)
    {
        var names = new List<string>();

        foreach (var binding in action.bindings)
        {
            if (!binding.isPartOfComposite ||
                !string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsDevice(binding, device))
                continue;

            AddUnique(names, DisplayName(binding, device));
        }

        return names;
    }

    static bool IsDevice(InputBinding binding, Device device)
    {
        string path = binding.effectivePath;

        if (string.IsNullOrEmpty(path))
            return false;

        return device == Device.Keyboard
            ? path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) ||
              path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase)
            : path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase);
    }

    static string DisplayName(InputBinding binding, Device device)
    {
        string path = binding.effectivePath;

        if (device == Device.Gamepad)
        {
            int slash = path.IndexOf('/');
            string controlPath = slash >= 0 ? path.Substring(slash + 1) : path;
            string[] parts = controlPath.Split('/');

            if (gamepadNames.TryGetValue(parts[0], out string friendly))
                return parts.Length > 1
                    ? $"{friendly}/{Capitalize(parts[1])}"
                    : friendly;
        }

        return InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    static string CombinePair(string left, string right)
    {
        int leftSlash = left.LastIndexOf('/');
        int rightSlash = right.LastIndexOf('/');

        if (leftSlash > 0 && rightSlash > 0 &&
            left.Substring(0, leftSlash) == right.Substring(0, rightSlash))
            return left.Substring(0, leftSlash);

        int leftSpace = left.LastIndexOf(' ');
        int rightSpace = right.LastIndexOf(' ');

        if (leftSpace > 0 && rightSpace > 0 &&
            left.Substring(leftSpace) == right.Substring(rightSpace))
            return $"{left.Substring(0, leftSpace)} / {right.Substring(0, rightSpace)}{left.Substring(leftSpace)}";

        return $"{left} / {right}";
    }

    static string Capitalize(string s)
    {
        return string.IsNullOrEmpty(s)
            ? s
            : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    static void AddUnique(List<string> list, string value)
    {
        if (!string.IsNullOrEmpty(value) && !list.Contains(value))
            list.Add(value);
    }

    static string Join(List<string> names)
    {
        return names.Count == 0 ? None : string.Join(", ", names);
    }
}
