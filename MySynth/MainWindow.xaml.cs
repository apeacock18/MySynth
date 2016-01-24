using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using NAudio.Utils;
using NAudio.Wave.SampleProviders;
using NAudio.Gui;
using NAudio.Dsp;

namespace MySynth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOut driverOut;
        private ADSRSignalGenerator wave;
        private DoubleCollection frqs;

        private SignalGenerator osc1;
        private SignalGenerator osc2;
        private SignalGenerator osc3;
        private MixingSampleProvider mixer;

        private WaveformPainter painter = new WaveformPainter();

        private const double fMin = 27.5;
        private const double fMax = 2000;
        private const float SampleRate = 44100f;
        private double frqLog;
        bool envEnabled;

        public MainWindow()
        {
            frqLog = Math.Log10(fMax / fMin);

            // Init Audio
            driverOut = new WaveOut();
            driverOut.DesiredLatency = 100;
            wave = new ADSRSignalGenerator();
            osc1 = new SignalGenerator();
            osc2 = new SignalGenerator();
            osc3 = new SignalGenerator();
            driverOut.Init(wave);
            envEnabled = false;

            // Init UI
            InitializeComponent();
        }

        public void WavePlay()
        {
            if (driverOut != null & driverOut.PlaybackState == PlaybackState.Stopped)
            {
                if (envEnabled)
                    wave.env.Gate(true);

                driverOut.Play();
                playbtn.IsEnabled = false;
                stopbtn.IsEnabled = true;
            }
        }

        public void WaveStop()
        {
            if (driverOut != null & driverOut.PlaybackState == PlaybackState.Playing)
            {
                wave.env.Gate(false);
                driverOut.Stop();
                playbtn.IsEnabled = true;
                stopbtn.IsEnabled = false;
            }
        }


        private void playbtn_Click(object sender, RoutedEventArgs e)
        {
            WavePlay();
        }

        private void stopbtn_Click(object sender, RoutedEventArgs e)
        {
            WaveStop();
        }

        private void waveMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (waveMenu.SelectedIndex)
            {
                case 0:
                    StartWave(SignalGeneratorType.Sin);
                    break;
                case 1:
                    StartWave(SignalGeneratorType.Square);
                    break;
                case 2:
                    StartWave(SignalGeneratorType.Triangle);
                    break;
                case 3:
                    StartWave(SignalGeneratorType.SawTooth);
                    break;
                case 4:
                    wave.Type = SignalGeneratorType.Pink;
                    InitMixer();
                    break;
                default:
                    break;
            }
        }

        private void StartWave(SignalGeneratorType type)
        {
            SignalGeneratorType t = wave.Type;
            wave.Type = type;
            if (t != SignalGeneratorType.Pink)
            {
                driverOut.Stop();
                driverOut.Dispose();
                driverOut = new WaveOut();
                driverOut.Init(wave);
                driverOut.Play();
            }
        }

        private void InitMixer()
        {
            InitOsc();

            List<ISampleProvider> samples =  new List<ISampleProvider>();
            if (osc2Menu.SelectedIndex != -1)
                samples.Add(osc2);
            if (osc1Menu.SelectedIndex != -1)
                samples.Add(osc1);
            if (osc3Menu.SelectedIndex != -1)
                samples.Add(osc3);

            mixer = new MixingSampleProvider(samples);

            driverOut.Stop();
            driverOut.Dispose();
            driverOut = new WaveOut();
            driverOut.Init(mixer);
            WavePlay();
        }

        private void InitOsc()
        {
            
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            wave.Gain = Decibels.DecibelsToLinear(e.NewValue);
        }

        private void frqSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TickToFrq();
        }

        private int ToOctave(int index)
        {
            return (int)(10.35f * index);
        }

        private void TickToFrq()
        {
            if (precisionList.SelectedIndex <= 0)
            {
                wave.Frequency = frqSlider.Value;
            }
            else
            {
                wave.Frequency = frqs[(int)frqSlider.Value];
            }
            frqValue.Text = wave.Frequency.ToString();
        }

        private void precisionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
                switch (precisionList.SelectedIndex)
                {
                    case 0:
                        frqSlider.IsSnapToTickEnabled = false;
                        frqSlider.Maximum = 2000;
                        break;
                    case 1:
                        frqSlider.IsSnapToTickEnabled = true;
                        frqs = SetTickFrq(2);
                        SetTicks();
                        break;
                    case 2:
                        frqSlider.IsSnapToTickEnabled = true;
                        frqs = SetTickFrq(1.5);
                        SetTicks();
                        break;
                    case 3:
                        frqSlider.IsSnapToTickEnabled = true;
                        frqs = SetTickFrq(1.333333);
                        SetTicks();
                        break;
                    case 4:
                        frqSlider.IsSnapToTickEnabled = true;
                        frqs = SetTickFrq(1.666666);
                        SetTicks();
                        break;
                    case 5:
                        frqSlider.IsSnapToTickEnabled = true;
                        frqs = SetTickFrq(1.083333);
                        SetTicks();
                        break;
                    case 6:
                        frqSlider.IsSnapToTickEnabled = true;
                        frqs = SetTickFrq(1.0416666);
                        SetTicks();
                        break;
                }
        }

        private void SetTicks()
        {
            frqSlider.Ticks.Clear();
            for (int i = 0; i < frqs.Count; i++)
            {
                frqSlider.Ticks.Add(i);
            }
            frqSlider.Minimum = 0;
            frqSlider.Maximum = frqSlider.Ticks.Count - 1;
        }

        private DoubleCollection SetTickFrq(double octave)
        {
            DoubleCollection ticks = new DoubleCollection();
            double tick = fMin;
            while (tick < fMax)
            {
                ticks.Add(tick);
                tick *= octave;
            }
            return ticks;
        }

        private void piano_MouseDown(object sender, MouseButtonEventArgs e)
        {
            wave.Frequency = Convert.ToDouble(((Button)sender).Content);
            WavePlay();
            
        }

        private void piano_MouseUp(object sender, MouseButtonEventArgs e)
        {
            WaveStop();
        }

        private void piano_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                wave.Frequency = Convert.ToDouble(((Button)sender).Content);
            }
        }

        private void CreateMixerWave()
        {
            osc1 = new SignalGenerator();
            switch (osc1Menu.SelectedIndex)
            {
                case 0:
                    osc1.Type = SignalGeneratorType.Sin;
                    break;
                case 1:
                    osc1.Type = SignalGeneratorType.Square;
                    break;
                case 2:
                    osc1.Type = SignalGeneratorType.Triangle;
                    break;
                case 3:
                    osc1.Type = SignalGeneratorType.SawTooth;
                    break;
                default:
                    break;
            }

            osc2 = new SignalGenerator();
            switch (osc2Menu.SelectedIndex)
            {
                case 0:
                    osc2.Type = SignalGeneratorType.Sin;
                    break;
                case 1:
                    osc2.Type = SignalGeneratorType.Square;
                    break;
                case 2:
                    osc2.Type = SignalGeneratorType.Triangle;
                    break;
                case 3:
                    osc2.Type = SignalGeneratorType.SawTooth;
                    break;
                default:
                    break;
            }
            osc3 = new SignalGenerator();
            switch (osc3Menu.SelectedIndex)
            {
                case 0:
                    osc3.Type = SignalGeneratorType.Sin;
                    break;
                case 1:
                    osc3.Type = SignalGeneratorType.Square;
                    break;
                case 2:
                    osc3.Type = SignalGeneratorType.Triangle;
                    break;
                case 3:
                    osc3.Type = SignalGeneratorType.SawTooth;
                    break;
                default:
                    break;
            }
        }

        private void osc1Gain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            osc1.Gain = Decibels.DecibelsToLinear(e.NewValue);
            InitMixer();
        }

        private void osc2Gain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            osc2.Gain = Decibels.DecibelsToLinear(e.NewValue);
            InitMixer();
        }

        private void osc3Gain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            osc3.Gain = Decibels.DecibelsToLinear(e.NewValue);
            InitMixer();
        }

        private void osc1Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (osc1Menu.SelectedIndex)
            {
                case 0:
                    osc1.Type = SignalGeneratorType.Sin;
                    break;
                case 1:
                    osc1.Type = SignalGeneratorType.Square;
                    break;
                case 2:
                    osc1.Type = SignalGeneratorType.Triangle;
                    break;
                case 3:
                    osc1.Type = SignalGeneratorType.SawTooth;
                    break;
                default:
                    break;
            }
        }

        private void osc2Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (osc2Menu.SelectedIndex)
            {
                case 0:
                    osc2.Type = SignalGeneratorType.Sin;
                    break;
                case 1:
                    osc2.Type = SignalGeneratorType.Square;
                    break;
                case 2:
                    osc2.Type = SignalGeneratorType.Triangle;
                    break;
                case 3:
                    osc2.Type = SignalGeneratorType.SawTooth;
                    break;
                default:
                    break;
            }
        }

        private void osc3Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (osc3Menu.SelectedIndex)
            {
                case 0:
                    osc3.Type = SignalGeneratorType.Sin;
                    break;
                case 1:
                    osc3.Type = SignalGeneratorType.Square;
                    break;
                case 2:
                    osc3.Type = SignalGeneratorType.Triangle;
                    break;
                case 3:
                    osc3.Type = SignalGeneratorType.SawTooth;
                    break;
                default:
                    break;
            }
        }

        private void PhaseLeft_Checked(object sender, RoutedEventArgs e)
        {
            wave.PhaseReverse[0] = true;
        }

        private void PhaseLeft_Unchecked(object sender, RoutedEventArgs e)
        {
            wave.PhaseReverse[0] = false;
        }

        private void PhaseRight_Checked(object sender, RoutedEventArgs e)
        {
            wave.PhaseReverse[1] = true;
        }

        private void PhaseRight_Unchecked(object sender, RoutedEventArgs e)
        {
            wave.PhaseReverse[1] = false;
        }

        private void envOnOff_Click(object sender, RoutedEventArgs e)
        {
            if (envEnabled)
            {
                envEnabled = false;
                envOnOff.Background = null;
                envOnOff.Content = "Off";
            }
            else
            {
                envEnabled = true;
                envOnOff.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                envOnOff.Content = "On";
            }
        }

        private void decay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            wave.env.DecayRate = ((float)decay.Value * SampleRate);
        }

        private void sustain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            wave.env.SustainLevel = ((float)sustain.Value);
        }

        private void release_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            wave.env.ReleaseRate = ((float)release.Value * SampleRate);
        }

        private void attack_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            wave.env.AttackRate = ((float)attack.Value * SampleRate);
        }
    }

    public class ADSRSignalGenerator : ISampleProvider
    {
        public EnvelopeGenerator env { get; set; }

        private readonly WaveFormat waveFormat;

        // Random Number for the White Noise & Pink Noise Generator
        private readonly Random random = new Random();

        private readonly double[] pinkNoiseBuffer = new double[7];

        // Const Math
        private const double TwoPi = 2 * Math.PI;

        // Generator variable
        private int nSample;

        // Sweep Generator variable
        private double phi;

        /// <summary>
        /// Initializes a new instance for the Generator (Default :: 44.1Khz, 2 channels, Sinus, Frequency = 440, Gain = 1)
        /// </summary>
        public ADSRSignalGenerator()
            : this(44100, 2)
        {

        }

        public ADSRSignalGenerator(int sampleRate, int channel)
        {
            phi = 0;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);

            // Default
            Type = SignalGeneratorType.Sin;
            Frequency = 440.0;
            Gain = 1;
            PhaseReverse = new bool[channel];
            SweepLengthSecs = 2;

            env = new EnvelopeGenerator();
            env.AttackRate = 0f;
            env.DecayRate = 0f;
            env.SustainLevel = .8f;
            env.ReleaseRate = 0f;
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        public double Frequency { get; set; }

        public double FrequencyLog
        {
            get { return Math.Log(Frequency); }
        }

        public double FrequencyEnd { get; set; }

        public double FrequencyEndLog
        {
            get { return Math.Log(FrequencyEnd); }
        }

        public double Gain { get; set; }

        public bool[] PhaseReverse { get; private set; }

        public SignalGeneratorType Type { get; set; }

        public double SweepLengthSecs { get; set; }

        public int Read(float[] buffer, int offset, int count)
        {
            int outIndex = offset;

            // Generator current value
            double multiple;
            double sampleValue;
            double sampleSaw;

            // Complete Buffer
            for (int sampleCount = 0; sampleCount < count / waveFormat.Channels; sampleCount++)
            {
                switch (Type)
                {
                    case SignalGeneratorType.Sin:

                        // Sinus Generator

                        multiple = TwoPi * Frequency / waveFormat.SampleRate;
                        sampleValue = Gain * Math.Sin(nSample * multiple);

                        nSample++;

                        break;


                    case SignalGeneratorType.Square:

                        // Square Generator

                        multiple = 2 * Frequency / waveFormat.SampleRate;
                        sampleSaw = ((nSample * multiple) % 2) - 1;
                        sampleValue = sampleSaw > 0 ? Gain : -Gain;

                        nSample++;
                        break;

                    case SignalGeneratorType.Triangle:

                        // Triangle Generator

                        multiple = 2 * Frequency / waveFormat.SampleRate;
                        sampleSaw = ((nSample * multiple) % 2);
                        sampleValue = 2 * sampleSaw;
                        if (sampleValue > 1)
                            sampleValue = 2 - sampleValue;
                        if (sampleValue < -1)
                            sampleValue = -2 - sampleValue;

                        sampleValue *= Gain;

                        nSample++;
                        break;

                    case SignalGeneratorType.SawTooth:

                        // SawTooth Generator

                        multiple = 2 * Frequency / waveFormat.SampleRate;
                        sampleSaw = ((nSample * multiple) % 2) - 1;
                        sampleValue = Gain * sampleSaw;

                        nSample++;
                        break;

                    case SignalGeneratorType.White:

                        // White Noise Generator
                        sampleValue = (Gain * NextRandomTwo());
                        break;

                    case SignalGeneratorType.Pink:

                        // Pink Noise Generator

                        double white = NextRandomTwo();
                        pinkNoiseBuffer[0] = 0.99886 * pinkNoiseBuffer[0] + white * 0.0555179;
                        pinkNoiseBuffer[1] = 0.99332 * pinkNoiseBuffer[1] + white * 0.0750759;
                        pinkNoiseBuffer[2] = 0.96900 * pinkNoiseBuffer[2] + white * 0.1538520;
                        pinkNoiseBuffer[3] = 0.86650 * pinkNoiseBuffer[3] + white * 0.3104856;
                        pinkNoiseBuffer[4] = 0.55000 * pinkNoiseBuffer[4] + white * 0.5329522;
                        pinkNoiseBuffer[5] = -0.7616 * pinkNoiseBuffer[5] - white * 0.0168980;
                        double pink = pinkNoiseBuffer[0] + pinkNoiseBuffer[1] + pinkNoiseBuffer[2] + pinkNoiseBuffer[3] + pinkNoiseBuffer[4] + pinkNoiseBuffer[5] + pinkNoiseBuffer[6] + white * 0.5362;
                        pinkNoiseBuffer[6] = white * 0.115926;
                        sampleValue = (Gain * (pink / 5));
                        break;

                    case SignalGeneratorType.Sweep:

                        // Sweep Generator
                        double f = Math.Exp(FrequencyLog + (nSample * (FrequencyEndLog - FrequencyLog)) / (SweepLengthSecs * waveFormat.SampleRate));

                        multiple = TwoPi * f / waveFormat.SampleRate;
                        phi += multiple;
                        sampleValue = Gain * (Math.Sin(phi));
                        nSample++;
                        if (nSample > SweepLengthSecs * waveFormat.SampleRate)
                        {
                            nSample = 0;
                            phi = 0;
                        }
                        break;

                    default:
                        sampleValue = 0.0;
                        break;
                }

                // Phase Reverse Per Channel
                for (int i = 0; i < waveFormat.Channels; i++)
                {
                    if (env.State != EnvelopeGenerator.EnvelopeState.Idle)
                    {
                        if (PhaseReverse[i])
                            buffer[outIndex++] = (float)-sampleValue * env.Process();
                        else
                            buffer[outIndex++] = (float)sampleValue * env.Process();
                    }
                    else
                    {
                        if (PhaseReverse[i])
                            buffer[outIndex++] = (float)-sampleValue;
                        else
                            buffer[outIndex++] = (float)sampleValue;
                    }
                }
            }
            return count;
        }

        private double NextRandomTwo()
        {
            return 2 * random.NextDouble() - 1;
        }

    }

    public class PianoKey : Button
    {
        public double Frequency { get; set; }
    }
}
