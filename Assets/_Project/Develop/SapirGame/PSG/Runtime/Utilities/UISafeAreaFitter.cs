using System;
using SapirGame.PSG.Runtime.Utilities.Logging;
using UnityEngine;

namespace SapirGame.PSG.Runtime.Utilities
{
    /// <summary>
    /// Defines the direction or edge for UI safe area adjustments.
    /// </summary>
    public enum FitmentType
    {
        /// <summary>Adjust to left edge of safe area.</summary>
        Left,
        /// <summary>Adjust to right edge of safe area.</summary>
        Right,
        /// <summary>Adjust to top edge of safe area.</summary>
        Top,
        /// <summary>Adjust to bottom edge of safe area.</summary>
        Bottom,
        /// <summary>Adjust both left and right edges simultaneously.</summary>
        Horizontal,
        /// <summary>Adjust both top and bottom edges simultaneously.</summary>
        Vertical
    }

    /// <summary>
    /// Provides extension methods for adapting UI elements to device-specific safe areas,
    /// accounting for notches, rounded corners, and system UI elements.
    /// </summary>
    public static class UISafeAreaFitter
    {
        /// <summary>
        /// Expands the RectTransform to fill the entire safe area of the screen.
        /// Commonly used for full-screen UI elements that need to avoid system overlays.
        /// </summary>
        /// <param name="trans">The RectTransform to adjust.</param>
        public static void FitInSafeArea(this RectTransform trans)
        {
            Rect safeAreaRect = Screen.safeArea;
            Vector2 anchorMin = safeAreaRect.position;
            Vector2 anchorMax = safeAreaRect.position + safeAreaRect.size;

            // iOS-specific adjustment for status bar height
#if UNITY_IOS
            anchorMin.y /= 3;
#endif

            Log.Default.D("SAFE AREA",$"anchor min {anchorMin} max {anchorMax}");

            // Convert from pixel coordinates to normalized anchor coordinates (0-1 range)
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            trans.anchorMin = anchorMin;
            trans.anchorMax = anchorMax;

            Log.Default.D("SAFE AREA",
                $"applied to {trans.name}: x={safeAreaRect.x}, y={safeAreaRect.y}, w={safeAreaRect.width}, h={safeAreaRect.height} on full extents w={Screen.width}, h={Screen.height}");
        }

        /// <summary>
        /// Adjusts only specific edges of a RectTransform to match the safe area boundaries.
        /// Useful for UI elements that need to align to particular screen edges while maintaining original size.
        /// </summary>
        /// <param name="trans">The RectTransform to adjust.</param>
        /// <param name="fitmentType">Which edge(s) to align with the safe area.</param>
        /// <param name="offset">Additional spacing from the safe area edge in pixels.</param>
        public static void FitInSafeArea(this RectTransform trans, FitmentType fitmentType, float offset = 0)
        {
            Rect safeAreaRect = Screen.safeArea;

            // Early exit if device doesn't have a recognizable safe area
            if (safeAreaRect.height + 1 > Screen.height || safeAreaRect.width + 1 > Screen.width)
                return;

            // Calculate insets from screen edges to safe area boundaries
            safeAreaRect.height += (Screen.height - safeAreaRect.height) / 2;
            safeAreaRect.width += (Screen.width - safeAreaRect.width) / 2;

            // Apply offset to appropriate dimension based on fitment direction
            if (fitmentType == FitmentType.Left || fitmentType == FitmentType.Right)
                safeAreaRect.width += offset;

            if (fitmentType == FitmentType.Top || fitmentType == FitmentType.Bottom)
                safeAreaRect.height += offset;

            Vector2 anchorMin = safeAreaRect.position;
            Vector2 anchorMax = safeAreaRect.position + safeAreaRect.size;

            // Adjust only specified anchors while preserving others
            switch (fitmentType) {
                case FitmentType.Left:
                    // Anchor to left safe area edge, right edge stays unchanged
                    anchorMin.x = 1 - safeAreaRect.width / Screen.width;
                    anchorMin.y = trans.anchorMin.y;
                    anchorMax.x = trans.anchorMax.x;
                    anchorMax.y = trans.anchorMax.y;
                    break;
                case FitmentType.Right:
                    // Anchor to right safe area edge, left edge stays unchanged
                    anchorMax.x = safeAreaRect.width / Screen.width;
                    anchorMin.x = trans.anchorMin.x;
                    anchorMin.y = trans.anchorMin.y;
                    anchorMax.y = trans.anchorMax.y;
                    break;
                case FitmentType.Top:
                    // Anchor to top safe area edge, bottom edge stays unchanged
                    anchorMax.y = safeAreaRect.height / Screen.height;
                    anchorMin.x = trans.anchorMin.x;
                    anchorMin.y = trans.anchorMin.y;
                    anchorMax.x = trans.anchorMax.x;
                    break;
                case FitmentType.Bottom:
                    // Anchor to bottom safe area edge, top edge stays unchanged
                    anchorMin.y = 1 - safeAreaRect.height / Screen.height;
                    anchorMin.x = trans.anchorMin.x;
                    anchorMax.y = trans.anchorMax.y;
                    anchorMax.x = trans.anchorMax.x;
                    break;
                case FitmentType.Horizontal:
                    // Anchor both left and right edges to safe area
                    anchorMin.x = 1 - safeAreaRect.width / Screen.width;
                    anchorMax.x = safeAreaRect.width / Screen.width;
                    anchorMin.y = trans.anchorMin.y;
                    anchorMax.y = trans.anchorMax.y;
                    break;
                case FitmentType.Vertical:
                    // Anchor both top and bottom edges to safe area
                    anchorMin.y = 1 - safeAreaRect.height / Screen.height;
                    anchorMax.y = safeAreaRect.height / Screen.height;
                    anchorMin.x = trans.anchorMin.x;
                    anchorMax.x = trans.anchorMax.x;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(fitmentType), fitmentType, null);
            }

            trans.anchorMin = anchorMin;
            trans.anchorMax = anchorMax;

            Log.Default.D("SAFE AREA",
                $"applied to {trans.name}: x={safeAreaRect.x}, y={safeAreaRect.y}, w={safeAreaRect.width}, h={safeAreaRect.height} on full extents w={Screen.width}, h={Screen.height}");
        }

        /// <summary>
        /// Shifts a RectTransform's position without resizing it, moving it away from safe area edges.
        /// Useful for floating UI elements that need to avoid system UI overlaps.
        /// </summary>
        /// <param name="trans">The RectTransform to reposition.</param>
        /// <param name="fitmentType">Direction to move the element away from the safe area edge.</param>
        /// <param name="offset">Additional movement distance in pixels.</param>
        public static void MoveInSafeArea(this RectTransform trans, FitmentType fitmentType, float offset = 0)
        {
            Rect safeAreaRect = Screen.safeArea;

            // Early exit if device doesn't have a recognizable safe area
            if (safeAreaRect.height + 1 > Screen.height || safeAreaRect.width + 1 > Screen.width)
                return;

            // Calculate the inset amount (space between screen edge and safe area)
            safeAreaRect.height = (Screen.height - safeAreaRect.height) / 2;
            safeAreaRect.width = (Screen.width - safeAreaRect.width) / 2;

            // Apply offset to movement distance
            if (fitmentType == FitmentType.Left || fitmentType == FitmentType.Right)
                safeAreaRect.width += offset;

            if (fitmentType == FitmentType.Top || fitmentType == FitmentType.Bottom)
                safeAreaRect.height += offset;

            Vector2 pos = trans.anchoredPosition;

            // Move element away from specified edge by half the inset amount
            switch (fitmentType) {
                case FitmentType.Left:
                    // Move left to avoid left notch/cutout
                    pos.x -= safeAreaRect.width / 2 + offset;
                    break;
                case FitmentType.Right:
                    // Move right to avoid right notch/cutout
                    pos.x += safeAreaRect.width / 2 + offset;
                    break;
                case FitmentType.Top:
                    // Move down to avoid top notch/status bar
                    pos.y += safeAreaRect.height / 2 + offset;
                    break;
                case FitmentType.Bottom:
                    // Move up to avoid bottom home indicator
                    pos.y -= safeAreaRect.height / 2 + offset;
                    break;
            }

            trans.anchoredPosition = pos;

            Log.Default.D("SAFE AREA",
                $"applied to {trans.name}: x={safeAreaRect.x}, y={safeAreaRect.y}, w={safeAreaRect.width}, h={safeAreaRect.height} on full extents w={Screen.width}, h={Screen.height}");
        }
    }
}
