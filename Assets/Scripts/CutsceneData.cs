using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enthält alle Daten für eine Cutscene (Bilder, Animationen, Timing).
/// Erstelle neue Cutscenes im Unity Editor per: Rechtsklick → Create → FalseFriend → Cutscene Data
/// Ziehe dann Bilder in die Slides-Liste und konfiguriere die Animationen!
/// </summary>
[CreateAssetMenu(fileName = "NewCutscene", menuName = "FalseFriend/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    public enum SlideTransition
    {
        DipToBlack,     // Bildschirm wird schwarz, dann neues Bild
        Crossfade,      // Bilder überblenden direkt ineinander
    }

    /// <summary>
    /// Welche Animation ein Slide haben soll
    /// </summary>
    public enum SlideAnimation
    {
        KenBurnsZoomIn,     // Langsam reinzoomen + leicht pannen (klassisch!)
        KenBurnsZoomOut,    // Langsam rauszoomen
        PanLeft,            // Von rechts nach links pannen
        PanRight,           // Von links nach rechts pannen
        PanUp,              // Von unten nach oben pannen
        PanDown,            // Von oben nach unten pannen
        Static,             // Kein Effekt, Bild steht still
        SlowShake,          // Subtiles Zittern (für dramatische Momente)
    }

    /// <summary>
    /// Ein einzelner Slide (= ein Bild in der Cutscene)
    /// </summary>
    [System.Serializable]
    public class Slide
    {
        [Tooltip("Das Cutscene-Bild (als Sprite importieren!)")]
        public Sprite image;

        [Tooltip("Wie lange das Bild angezeigt wird (Sekunden)")]
        public float duration = 4f;

        [Tooltip("Welche Animation das Bild hat")]
        public SlideAnimation animation = SlideAnimation.KenBurnsZoomIn;

        [Tooltip("Wie stark der Zoom-Effekt ist (1.0 = kein Zoom, 1.2 = 20% reinzoomen)")]
        [Range(1.0f, 1.5f)]
        public float zoomIntensity = 1.15f;

        [Tooltip("Wie stark das Panning ist (0.05 = subtil, 0.2 = stark)")]
        [Range(0f, 0.5f)]
        public float panSpeed = 0.1f;

        [Tooltip("Optionaler Text der eingeblendet wird (z.B. 'Kapitel 1: Der Wald')")]
        [TextArea(1, 3)]
        public string captionText = "";

        [Tooltip("Optionaler Sound-Effekt beim Einblenden dieses Slides")]
        public AudioClip soundEffect;

        [Tooltip("Ob der Spieler mit Klick/Leertaste zum nächsten Bild springen kann")]
        public bool allowSkip = true;

        [Tooltip("Wie der Übergang ZUM NÄCHSTEN Bild aussehen soll")]
        public SlideTransition transitionToNext = SlideTransition.DipToBlack;

        [Tooltip("Wie lange dieser Übergang zum nächsten Bild dauert (Sekunden)")]
        public float transitionDuration = 1.0f;
    }

    [Header("Slides (Bilder hier reinziehen!)")]
    [Tooltip("Die Liste aller Bilder in dieser Cutscene, in Reihenfolge")]
    public List<Slide> slides = new List<Slide>();

    [Header("Überblend-Einstellungen")]
    [Tooltip("Wie lange jedes Bild einfadet (Sekunden)")]
    public float fadeInDuration = 1.0f;

    [Tooltip("Wie lange jedes Bild ausfadet (Sekunden)")]
    public float fadeOutDuration = 0.8f;

    [Tooltip("Pause zwischen zwei Slides (Sekunden, Bildschirm ist kurz schwarz)")]
    public float pauseBetweenSlides = 0.3f;

    [Header("Cinematische Effekte")]
    [Tooltip("Schwarze Balken oben/unten wie im Kino (Letterbox)")]
    public bool showLetterbox = true;

    [Tooltip("Höhe der schwarzen Balken (0 = keine, 0.08 = standard, 0.15 = extrem breit)")]
    [Range(0f, 0.15f)]
    public float letterboxSize = 0.08f;

    [Tooltip("Subtile Staub-Partikel für Atmosphäre")]
    public bool showParticles = true;

    [Header("Audio")]
    [Tooltip("Hintergrundmusik während der gesamten Cutscene (optional)")]
    public AudioClip backgroundMusic;

    [Tooltip("Lautstärke der Hintergrundmusik")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
}
