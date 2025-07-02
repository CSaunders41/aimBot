using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ImGuiNET;
using ExileCore;
using ExileCore.Shared.Nodes;
using SharpDX;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;

namespace AimBot.Utilities
{
    public class ImGuiExtension
    {
        // Note: This will need to be called with a GameController instance
        public static ImGuiVector4 CenterWindow(int width, int height, GameController gameController)
        {
            var centerPos = gameController.Window.GetWindowRectangle().Center;
            return new ImGuiVector4(width + centerPos.X - width / 2, height + centerPos.Y - height / 2, width, height);
        }

        public static void ToolTip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(text);
            }
        }

        public static bool BeginWindow(string title, int x, int y, int width, int height, bool autoResize = false)
        {
            ImGui.SetNextWindowPos(new ImGuiVector2(width + x, height + y), ImGuiCond.Appearing, new ImGuiVector2(1, 1));
            ImGui.SetNextWindowSize(new ImGuiVector2(width, height), ImGuiCond.Appearing);
            return ImGui.Begin(title, autoResize ? ImGuiWindowFlags.AlwaysAutoResize : ImGuiWindowFlags.None);
        }
        
        public static bool BeginWindowCenter(string title, int width, int height, bool autoResize = false, GameController gameController = null)
        {
            if (gameController == null) return false;
            
            var size = CenterWindow(width, height, gameController);
            ImGui.SetNextWindowPos(new ImGuiVector2(size.X, size.Y), ImGuiCond.Appearing, new ImGuiVector2(1, 1));
            ImGui.SetNextWindowSize(new ImGuiVector2(size.Z, size.W), ImGuiCond.Appearing);
            return ImGui.Begin(title, autoResize ? ImGuiWindowFlags.AlwaysAutoResize : ImGuiWindowFlags.None);
        }

        // Int Sliders
        public static int IntSlider(string labelString, int value, int minValue, int maxValue)
        {
            var refValue = value;
            ImGui.SliderInt(labelString, ref refValue, minValue, maxValue);
            return refValue;
        }

        public static int IntSlider(string labelString, string sliderString, int value, int minValue, int maxValue)
        {
            var refValue = value;
            ImGui.SliderInt(labelString, ref refValue, minValue, maxValue, sliderString);
            return refValue;
        }

        public static int IntSlider(string labelString, RangeNode<int> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderInt(labelString, ref refValue, setting.Min, setting.Max);
            return refValue;
        }

        public static int IntSlider(string labelString, string sliderString, RangeNode<int> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderInt(labelString, ref refValue, setting.Min, setting.Max, sliderString);
            return refValue;
        }

        public static int IntDrag(string labelString, RangeNode<int> setting)
        {
            var refValue = setting.Value;
            ImGui.DragInt(labelString, ref refValue, 0.1f, setting.Min, setting.Max);
            return refValue;
        }

        public static int IntDrag(string labelString, string sliderString, RangeNode<int> setting)
        {
            var refValue = setting.Value;
            ImGui.DragInt(labelString, ref refValue, 0.1f, setting.Min, setting.Max, sliderString);
            return refValue;
        }

        // float Sliders
        public static float FloatSlider(string labelString, float value, float minValue, float maxValue)
        {
            var refValue = value;
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue);
            return refValue;
        }

        public static float FloatSlider(string labelString, float value, float minValue, float maxValue, float power)
        {
            var refValue = value;
            // Note: Power parameter not supported in newer ImGui.NET, using regular slider
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, float value, float minValue, float maxValue)
        {
            var refValue = value;
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue, sliderString);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, float value, float minValue, float maxValue, float power)
        {
            var refValue = value;
            // Note: Power parameter not supported in newer ImGui.NET, using regular slider
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue, sliderString);
            return refValue;
        }

        public static float FloatSlider(string labelString, RangeNode<float> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max);
            return refValue;
        }

        public static float FloatSlider(string labelString, RangeNode<float> setting, float power)
        {
            var refValue = setting.Value;
            // Note: Power parameter not supported in newer ImGui.NET, using regular slider
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, RangeNode<float> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max, sliderString);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, RangeNode<float> setting, float power)
        {
            var refValue = setting.Value;
            // Note: Power parameter not supported in newer ImGui.NET, using regular slider
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max, sliderString);
            return refValue;
        }

        // Checkboxes
        public static bool Checkbox(string labelString, bool boolValue)
        {
            ImGui.Checkbox(labelString, ref boolValue);
            return boolValue;
        }

        public static bool Checkbox(string labelString, bool boolValue, out bool outBool)
        {
            ImGui.Checkbox(labelString, ref boolValue);
            outBool = boolValue;
            return boolValue;
        }

        // Hotkey Selector - simplified without WinApi dependency
        public static IEnumerable<Keys> KeyCodes() => Enum.GetValues(typeof(Keys)).Cast<Keys>();

        public static Keys HotkeySelector(string buttonName, string popupTitle, Keys currentKey)
        {
            if (ImGui.Button($"{buttonName}: {currentKey} ")) ImGui.OpenPopup(popupTitle);
            if (ImGui.BeginPopupModal(popupTitle, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Press a key to set as {buttonName}");
                ImGui.Text("Note: Key detection simplified for ExileCore compatibility");
                
                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.EndPopup();
            }

            return currentKey;
        }

        // Color Pickers
        public static Color ColorPicker(string labelName, Color inputColor)
        {
            var color = inputColor.ToVector4();
            var colorToVect4 = new ImGuiVector4(color.X, color.Y, color.Z, color.W);
            if (ImGui.ColorEdit4(labelName, ref colorToVect4, ImGuiColorEditFlags.AlphaBar))
            {
                return new Color(colorToVect4.X, colorToVect4.Y, colorToVect4.Z, colorToVect4.W);
            }
            return inputColor;
        }
    }
}