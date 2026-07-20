using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Corkboard-style pin-and-string system. Click a pin to start drawing a rubber string that
/// follows the cursor, then click a second pin to lock the connection in place.
/// </summary>
public class Corkboard_LineRenderer : MonoBehaviour
{
    [Header("Pins")]
    [Tooltip("Any UI element with a RectTransform can be a pin - Toggle, Button, Image, etc.")]
    [SerializeField] private List<RectTransform> pins = new List<RectTransform>();

    [Header("Line Renderer Template")]
    [Tooltip("Prefab or scene GameObject with a LineRenderer component. Its material, color and width define the string's look.")]
    [SerializeField] private LineRenderer linePrefab;

    [Header("Rubber String")]
    [Range(6, 60)]
    [SerializeField] private int lineSegments = 24;
    [Tooltip("Downward sag at the midpoint (in world units). 0 = perfectly straight.")]
    [Range(0.01f, 2f)]
    [SerializeField] private float sagAmount = 20f;
    [Tooltip("Spring stiffness that pulls the string toward its rest shape. Higher = tauter / snappier.")]
    [SerializeField] private float springStiffness = 150f;
    [Tooltip("Spring damping. Higher = less wobble / oscillation.")]
    [SerializeField] private float springDamping = 12f;

    [Header("Events")]
    [Tooltip("Fires the moment the cursor enters a different pin while a string is being drawn.")]
    [SerializeField] private UnityEvent onPinTouched;
    [Tooltip("Fires when the player clicks a second pin and the connection is locked in.")]
    [SerializeField] private UnityEvent onConnectionMade;

    // Active drawing state
    private RectTransform sourcePin;      // pin we started drawing from
    private LineRenderer activeLine;      // preview line while dragging
    private RectTransform lastHoveredPin; // for hover-enter detection

    // Spring physics for the sag point (drives the "rubber" wobble)
    private Vector3 sagPoint;
    private Vector3 sagVelocity;

    // Cached
    private Canvas canvas;
    private Camera canvasCam;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        // ScreenPointToWorldPointInRectangle / RectangleContainsScreenPoint want a null camera
        // for Overlay canvases and the canvas camera otherwise.
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCam = canvas.worldCamera;
        }

        // If the assigned linePrefab is a scene object (not a project prefab), hide the template
        // so only Instantiated clones are visible.
        if (linePrefab != null && linePrefab.gameObject.scene.IsValid())
        {
            linePrefab.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Mouse.current == null || canvas == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        RectTransform hovered = FindPinUnderMouse(mouseScreen);

        // ---- Hover-enter event (only while actively drawing) ----
        if (sourcePin != null && hovered != null && hovered != sourcePin && hovered != lastHoveredPin)
        {
            onPinTouched?.Invoke();
        }
        lastHoveredPin = hovered;

        // ---- Click handling ----
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick(hovered, mouseScreen);
        }

        // ---- Update the preview line's shape with spring physics each frame ----
        if (sourcePin != null && activeLine != null)
        {
            // If cursor is over another pin, snap the end to that pin for a magnetic feel.
            Vector3 endPos = (hovered != null && hovered != sourcePin)
                ? hovered.position
                : ScreenToWorld(mouseScreen);

            UpdateSpringLine(sourcePin.position, endPos);
        }
    }

    private void HandleClick(RectTransform hovered, Vector2 mouseScreen)
    {
        // No active drawing - start one if the click landed on a pin.
        if (sourcePin == null)
        {
            if (hovered == null) return;
            if (linePrefab == null)
            {
                Debug.LogWarning($"[{name}] Corkboard_LineRenderer: Line Prefab is not assigned.", this);
                return;
            }

            sourcePin = hovered;
            activeLine = Instantiate(linePrefab, transform);
            activeLine.gameObject.SetActive(true);
            activeLine.positionCount = lineSegments + 1;

            // Init spring at the resting position so the first frame doesn't yank.
            Vector3 endPos = ScreenToWorld(mouseScreen);
            sagPoint = (sourcePin.position + endPos) * 0.5f + Vector3.down * sagAmount;
            sagVelocity = Vector3.zero;
            DrawBezier(activeLine, sourcePin.position, sagPoint, endPos);
            return;
        }

        // Already drawing - either complete the connection or cancel.
        if (hovered != null && hovered != sourcePin)
        {
            // Successful connection: snap the sag to rest for a clean settled shape.
            Vector3 endPos = hovered.position;
            sagPoint = (sourcePin.position + endPos) * 0.5f + Vector3.down * sagAmount;
            DrawBezier(activeLine, sourcePin.position, sagPoint, endPos);
            onConnectionMade?.Invoke();
        }
        else
        {
            // Clicked empty space or the same pin - cancel and remove the preview line.
            if (activeLine != null) Destroy(activeLine.gameObject);
        }

        // Reset drawing state either way. Completed lines stay in the scene as their own GameObjects.
        sourcePin = null;
        activeLine = null;
        lastHoveredPin = null;
    }

    private void UpdateSpringLine(Vector3 start, Vector3 end)
    {
        // Spring-damper physics: pulls the sag point toward (midpoint + downward sag).
        // Fast cursor moves make the sag lag, giving the classic rubber-band whip.
        Vector3 restPoint = (start + end) * 0.5f + Vector3.down * sagAmount;
        Vector3 force = (restPoint - sagPoint) * springStiffness - sagVelocity * springDamping;
        sagVelocity += force * Time.deltaTime;
        sagPoint += sagVelocity * Time.deltaTime;

        DrawBezier(activeLine, start, sagPoint, end);
    }

    private void DrawBezier(LineRenderer line, Vector3 start, Vector3 control, Vector3 end)
    {
        int count = lineSegments + 1;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)lineSegments;
            float u = 1f - t;
            line.SetPosition(i, (u * u) * start + (2f * u * t) * control + (t * t) * end);
        }
    }

    private RectTransform FindPinUnderMouse(Vector2 mouseScreen)
    {
        for (int i = 0; i < pins.Count; i++)
        {
            RectTransform pin = pins[i];
            if (pin == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(pin, mouseScreen, canvasCam))
            {
                return pin;
            }
        }
        return null;
    }

    private Vector3 ScreenToWorld(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform, screen, canvasCam, out Vector3 world);
        return world;
    }
}
