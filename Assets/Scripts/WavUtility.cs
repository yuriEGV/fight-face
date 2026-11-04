using System;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Utilidad autónoma pura en C# para codificar y decodificar archivos de audio WAV PCM (16-bit).
    /// Permite guardar grabaciones de micrófono y cargarlas directamente en AudioClips sin dependencias nativas.
    /// </summary>
    public static class WavUtility
    {
        public static byte[] FromAudioClip(AudioClip clip)
        {
            if (clip == null) return null;

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int channels = clip.channels;
                int frequency = clip.frequency;
                int samples = clip.samples;
                float[] data = new float[samples * channels];
                clip.GetData(data, 0);

                int byteRate = frequency * channels * 2;
                short blockAlign = (short)(channels * 2);
                int subChunk2Size = samples * channels * 2;
                int chunkSize = 36 + subChunk2Size;

                // RIFF chunk descriptor
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(chunkSize);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

                // "fmt " subchunk
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // SubChunk1Size for PCM
                writer.Write((short)1); // AudioFormat: 1 = PCM
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write((short)16); // BitsPerSample: 16-bit

                // "data" subchunk
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(subChunk2Size);

                // Samples convertidos a 16-bit PCM little-endian
                for (int i = 0; i < data.Length; i++)
                {
                    short val = (short)Mathf.Clamp(data[i] * 32767f, -32768f, 32767f);
                    writer.Write(val);
                }

                return stream.ToArray();
            }
        }

        public static AudioClip ToAudioClip(byte[] wavBytes, string clipName = "VoiceClip")
        {
            if (wavBytes == null || wavBytes.Length < 44) return null;

            try
            {
                using (var stream = new MemoryStream(wavBytes))
                using (var reader = new BinaryReader(stream))
                {
                    string riff = new string(reader.ReadChars(4));
                    if (riff != "RIFF") return null;

                    int fileSize = reader.ReadInt32();
                    string wave = new string(reader.ReadChars(4));
                    if (wave != "WAVE") return null;

                    short channels = 1;
                    int sampleRate = 44100;
                    short bitsPerSample = 16;
                    byte[] audioData = null;

                    while (stream.Position < stream.Length - 8)
                    {
                        string chunkID = new string(reader.ReadChars(4));
                        int chunkSize = reader.ReadInt32();

                        if (chunkID == "fmt ")
                        {
                            short format = reader.ReadInt16();
                            channels = reader.ReadInt16();
                            sampleRate = reader.ReadInt32();
                            int byteRate = reader.ReadInt32();
                            short blockAlign = reader.ReadInt16();
                            bitsPerSample = reader.ReadInt16();

                            int extra = chunkSize - 16;
                            if (extra > 0 && stream.Position + extra <= stream.Length)
                            {
                                reader.ReadBytes(extra);
                            }
                        }
                        else if (chunkID == "data")
                        {
                            int bytesToRead = Mathf.Min(chunkSize, (int)(stream.Length - stream.Position));
                            audioData = reader.ReadBytes(bytesToRead);
                            break;
                        }
                        else
                        {
                            int bytesToSkip = Mathf.Min(chunkSize, (int)(stream.Length - stream.Position));
                            reader.ReadBytes(bytesToSkip);
                        }
                    }

                    if (audioData == null || audioData.Length == 0 || bitsPerSample != 16 || channels <= 0) return null;

                    int numSamples = audioData.Length / (2 * channels);
                    if (numSamples <= 0) return null;

                    float[] samples = new float[numSamples * channels];

                    for (int i = 0; i < samples.Length; i++)
                    {
                        short val = BitConverter.ToInt16(audioData, i * 2);
                        samples[i] = val / 32768f;
                    }

                    // Normalización automática de amplitud de pico (Peak Normalization)
                    // Garantiza que audios grabados a bajo volumen se escuchen con fuerza y nitidez
                    float peak = 0f;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        float abs = Mathf.Abs(samples[i]);
                        if (abs > peak) peak = abs;
                    }
                    if (peak > 0.02f && peak < 0.90f)
                    {
                        float gain = Mathf.Min(0.95f / peak, 5.0f);
                        for (int i = 0; i < samples.Length; i++)
                        {
                            samples[i] = Mathf.Clamp(samples[i] * gain, -1.0f, 1.0f);
                        }
                    }

                    AudioClip clip = AudioClip.Create(clipName, numSamples, channels, sampleRate, false);
                    clip.SetData(samples, 0);
                    return clip;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WavUtility] Error al decodificar WAV: {ex.Message}");
                return null;
            }
        }

        public static bool SaveToFile(AudioClip clip, string filePath)
        {
            if (clip == null || string.IsNullOrEmpty(filePath)) return false;

            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                byte[] bytes = FromAudioClip(clip);
                if (bytes == null || bytes.Length == 0) return false;

                File.WriteAllBytes(filePath, bytes);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WavUtility] Error al guardar archivo WAV en {filePath}: {ex.Message}");
                return false;
            }
        }

        public static AudioClip LoadFromFile(string filePath, string clipName = "VoiceClip")
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                return ToAudioClip(bytes, clipName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WavUtility] Error al cargar archivo WAV desde {filePath}: {ex.Message}");
                return null;
            }
        }

        public static AudioClip TrimAudioClip(AudioClip source, int recordedSamples)
        {
            if (source == null || recordedSamples <= 0) return source;

            int channels = source.channels;
            int totalSamples = source.samples;
            int targetSamples = Mathf.Clamp(recordedSamples, 100, totalSamples);

            float[] allData = new float[totalSamples * channels];
            source.GetData(allData, 0);

            float[] trimmed = new float[targetSamples * channels];
            Array.Copy(allData, trimmed, trimmed.Length);

            // Normalización automática de amplitud de pico (Peak Normalization)
            // Si el micrófono grabó bajito, amplifica hasta el 95% de pico sin distorsionar
            float peak = 0f;
            for (int i = 0; i < trimmed.Length; i++)
            {
                float abs = Mathf.Abs(trimmed[i]);
                if (abs > peak) peak = abs;
            }
            if (peak > 0.02f && peak < 0.90f)
            {
                float gain = Mathf.Min(0.95f / peak, 6.0f);
                for (int i = 0; i < trimmed.Length; i++)
                {
                    trimmed[i] = Mathf.Clamp(trimmed[i] * gain, -1.0f, 1.0f);
                }
            }

            AudioClip trimmedClip = AudioClip.Create(source.name + "_Trimmed", targetSamples, channels, source.frequency, false);
            trimmedClip.SetData(trimmed, 0);
            return trimmedClip;
        }
    }
}
