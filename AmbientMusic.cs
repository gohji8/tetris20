using Godot;
using System;

public partial class AmbientMusic : Node
{
    private const int SampleRate = 22050;
    private const double Tempo = 68.0;
    private const double RootFrequency = 98.0;
    private static readonly int[] ChordProgression = { 0, -5, -2, -7 };
    private static readonly int[] Melody = { 7, -1, -1, 3, -1, -1, 0, -1 };

    private AudioStreamPlayer player;
    private AudioStreamGeneratorPlayback playback;
    private long sampleClock;
    private long accentStartSample = -1;
    private long rotationSoundStartSample = -1;
    private long landingSoundStartSample = -1;
    private int level;

    public override void _Ready()
    {
        var stream = new AudioStreamGenerator
        {
            MixRate = SampleRate,
            BufferLength = 0.15f
        };

        player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = -9f
        };
        AddChild(player);
        player.Play();
        playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public override void _Process(double delta)
    {
        if (playback == null)
        {
            playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
            if (playback == null)
            {
                return;
            }
        }

        int availableFrames = playback.GetFramesAvailable();
        for (int i = 0; i < availableFrames; i++)
        {
            playback.PushFrame(GenerateSample());
            sampleClock++;
        }
    }

    public void SetLevel(int linesCleared)
    {
        level = Math.Max(0, linesCleared / 10);
    }

    public void PlayLineClearAccent()
    {
        accentStartSample = sampleClock;
    }

    public void PlayRotationSound()
    {
        rotationSoundStartSample = sampleClock;
    }

    public void PlayLandingSound()
    {
        landingSoundStartSample = sampleClock;
    }

    private Vector2 GenerateSample()
    {
        double time = (double)sampleClock / SampleRate;
        double beat = time * Tempo / 60.0;
        double bars = beat / 4.0;
        int progressionStep = (int)Math.Floor(bars / 4.0);
        int chordIndex = (progressionStep + level) % ChordProgression.Length;
        int previousChordIndex = (chordIndex + ChordProgression.Length - 1) % ChordProgression.Length;
        double chordPhase = bars % 4.0 / 4.0;
        double chordBlend = chordPhase < 0.16 ? SmoothStep(chordPhase / 0.16) : 1.0;

        double currentChord = ChordProgression[chordIndex];
        double previousChord = ChordProgression[previousChordIndex];
        double root = RootFrequency * SemitoneRatio(currentChord);
        double previousRoot = RootFrequency * SemitoneRatio(previousChord);

        double pad = BlendPads(time, previousRoot, root, chordBlend);
        double breath = 0.94 + 0.06 * Math.Sin(2.0 * Math.PI * time / 19.0);
        double sub = Math.Sin(2.0 * Math.PI * root * 0.5 * time) * 0.045;
        double melody = GenerateMelody(time, beat, root);
        double accent = GenerateLineClearAccent(time, root);
        double rotationSound = GenerateRotationSound();
        double landingSound = GenerateLandingSound();
        double fadeIn = SmoothStep(time / 3.0);

        double sample = (pad * breath + sub + melody + accent + rotationSound + landingSound) * fadeIn * 1.55;
        float output = (float)Math.Clamp(sample, -0.65, 0.65);
        return new Vector2(output, output);
    }

    private static double BlendPads(double time, double previousRoot, double root, double blend)
    {
        double previous = PadAt(time, previousRoot);
        double current = PadAt(time, root);
        return previous * (1.0 - blend) + current * blend;
    }

    private static double PadAt(double time, double root)
    {
        double third = root * SemitoneRatio(3.0);
        double fifth = root * SemitoneRatio(7.0);

        return Math.Sin(2.0 * Math.PI * root * time) * 0.052
            + Math.Sin(2.0 * Math.PI * third * time) * 0.037
            + Math.Sin(2.0 * Math.PI * fifth * time) * 0.031;
    }

    private static double GenerateMelody(double time, double beat, double root)
    {
        double barPhase = beat / 4.0;
        int noteIndex = (int)Math.Floor(barPhase) % Melody.Length;
        int semitones = Melody[noteIndex];
        if (semitones < 0)
        {
            return 0.0;
        }

        double noteAge = (barPhase - Math.Floor(barPhase)) * 4.0 * 60.0 / Tempo;
        double attack = SmoothStep(noteAge / 0.32);
        double release = Math.Exp(-noteAge * 0.8);
        double frequency = root * 2.0 * SemitoneRatio(semitones);
        return Math.Sin(2.0 * Math.PI * frequency * time) * attack * release * 0.018;
    }

    private double GenerateLineClearAccent(double time, double root)
    {
        if (accentStartSample < 0)
        {
            return 0.0;
        }

        double age = (double)(sampleClock - accentStartSample) / SampleRate;
        if (age >= 1.2)
        {
            accentStartSample = -1;
            return 0.0;
        }

        double attack = SmoothStep(age / 0.18);
        double release = Math.Exp(-age * 2.4);
        double frequency = root * 1.5;
        return Math.Sin(2.0 * Math.PI * frequency * time) * attack * release * 0.012;
    }

    private double GenerateRotationSound()
    {
        if (rotationSoundStartSample < 0)
        {
            return 0.0;
        }

        double age = (double)(sampleClock - rotationSoundStartSample) / SampleRate;
        if (age >= 0.11)
        {
            rotationSoundStartSample = -1;
            return 0.0;
        }

        double envelope = SmoothStep(age / 0.016) * Math.Exp(-age * 24.0);
        double tone = Math.Sin(2.0 * Math.PI * 420.0 * age)
            + Math.Sin(2.0 * Math.PI * 630.0 * age) * 0.2;
        return tone * envelope * 0.018;
    }

    private double GenerateLandingSound()
    {
        if (landingSoundStartSample < 0)
        {
            return 0.0;
        }

        double age = (double)(sampleClock - landingSoundStartSample) / SampleRate;
        if (age >= 0.18)
        {
            landingSoundStartSample = -1;
            return 0.0;
        }

        double frequency = 104.0 - 34.0 * Math.Min(1.0, age / 0.18);
        double envelope = SmoothStep(age / 0.018) * Math.Exp(-age * 17.0);
        return Math.Sin(2.0 * Math.PI * frequency * age) * envelope * 0.045;
    }

    private static double SemitoneRatio(double semitones)
    {
        return Math.Pow(2.0, semitones / 12.0);
    }

    private static double SmoothStep(double value)
    {
        double clamped = Math.Clamp(value, 0.0, 1.0);
        return clamped * clamped * (3.0 - 2.0 * clamped);
    }
}
