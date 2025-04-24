using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using System;

//                      Jets Text-To-Speech Inference
//                      =============================
//
// This file implements the Jets Text-to-speech model in Unity Sentis
// The model uses phenomes instead of raw text so you have to convert it first

namespace VirtualAiAssistant.Tts
{
    public class JetsTts : MonoBehaviour
    {
        //Set to true if we have put the phoneme_dict.txt in the Assets/StreamingAssets folder
        [SerializeField]
        private bool hasPhenomeDictionary = true;
        [SerializeField]
        private AudioSource audioSource;

        readonly string[] phonemes = new string[] {
            "<blank>", "<unk>", "AH0", "N", "T", "D", "S", "R", "L", "DH", "K", "Z", "IH1",
            "IH0", "M", "EH1", "W", "P", "AE1", "AH1", "V", "ER0", "F", ",", "AA1", "B",
            "HH", "IY1", "UW1", "IY0", "AO1", "EY1", "AY1", ".", "OW1", "SH", "NG", "G",
            "ER1", "CH", "JH", "Y", "AW1", "TH", "UH1", "EH2", "OW0", "EY2", "AO0", "IH2",
            "AE2", "AY2", "AA2", "UW0", "EH0", "OY1", "EY0", "AO2", "ZH", "OW2", "AE0", "UW2",
            "AH2", "AY0", "IY2", "AW2", "AA0", "\"", "ER2", "UH2", "?", "OY2", "!", "AW0",
            "UH0", "OY0", "..", "<sos/eos>" };

        private readonly string[] alphabet = "AE1 B K D EH1 F G HH IH1 JH K L M N AA1 P K R S T AH1 V W K Y Z".Split(' ');
        private readonly Dictionary<string, string> dict = new();

        //Can change pitch and speed with this for a slightly different voice:
        private const int SampleRate = 22050;

        private Model model;
        private Worker worker;
        private AudioClip clip;
        private bool hasPendingInference = false;
        private Tensor<float> outputTensor;

        private bool isPlayingSpeech;
        private int generatedSpeechCount;

        public event Action<float> ProcessCompleted;
        public event Action ProcessCancelled;
        public event Action SpeechCompleted;

        private void Awake()
        {
            LoadModel();
            ReadDictionary();
        }

        private void Update()
        {
            if (hasPendingInference
                && outputTensor != null
                && outputTensor.IsReadbackRequestDone())
            {
                int length = outputTensor.shape.length;
                var audioData = outputTensor.DownloadToArray();
                outputTensor.Dispose();

                if (audioData.Length > length)
                {
                    Array.Resize(ref audioData, length);
                }

                generatedSpeechCount++;
                CompleteInference(audioData);

                outputTensor = null;
                hasPendingInference = false;
            }

            if (isPlayingSpeech)
            {
                if (!audioSource.isPlaying)
                {
                    isPlayingSpeech = false;
                    SpeechCompleted?.Invoke();
                }
            }
        }

        public void TextToSpeech(string inputText)
        {
            if (hasPendingInference) return;

            string ptext;
            if (hasPhenomeDictionary)
            {
                ptext = TextToPhonemes(inputText);
                Debug.Log(ptext);
            }
            else
            {
                //If we have no phenome dictionary we can use one of these examples:
                // ptext = "DH AH0 K W IH1 K B R AW1 N F AA1 K S JH AH1 M P S OW1 V ER0 DH AH0 L EY1 Z IY0 D AO1 G .";
                ptext = "W AH1 N S AH0 P AA1 N AH0 T AY1 M , AH0 F R AA1 G M EH1 T AH0 P R IH1 N S EH0 S . DH AH0 F R AA1 G K IH1 S T DH AH0 P R IH1 N S EH0 S AH0 N D B IH0 K EY1 M AH0 P R IH1 N S .";
                //ptext = "D UW1 P L AH0 K EY2 T";
            }
            DoInference(ptext);
        }

        private void LoadModel()
        {
            model = ModelLoader.Load(Path.Join(Application.streamingAssetsPath, "jets-TTS/jets-text-to-speech.sentis"));
        }

        private void CreateWorker()
        {
            worker?.Dispose();
            worker = new Worker(model, BackendType.GPUCompute);
        }

        private void ReadDictionary()
        {
            if (!hasPhenomeDictionary) return;
            string[] words = File.ReadAllLines(Path.Join(Application.streamingAssetsPath, "jets-TTS/phoneme_dict.txt"));
            for (int i = 0; i < words.Length; i++)
            {
                string s = words[i];
                string[] parts = s.Split();
                if (parts[0] != ";;;") //ignore comments in file
                {
                    string key = parts[0];
                    dict.Add(key, s.Substring(key.Length + 2));
                }
            }
            // Add codes for punctuation to the dictionary
            dict.Add(",", ",");
            dict.Add(".", ".");
            dict.Add("!", "!");
            dict.Add("?", "?");
            dict.Add("\"", "\"");
            // You could add extra word pronounciations here e.g.
            //dict.Add("somenewword","[phonemes]");
        }

        public string ExpandNumbers(string text)
        {
            return text
                .Replace("0", " ZERO ")
                .Replace("1", " ONE ")
                .Replace("2", " TWO ")
                .Replace("3", " THREE ")
                .Replace("4", " FOUR ")
                .Replace("5", " FIVE ")
                .Replace("6", " SIX ")
                .Replace("7", " SEVEN ")
                .Replace("8", " EIGHT ")
                .Replace("9", " NINE ");
        }

        public string TextToPhonemes(string text)
        {
            string output = "";
            text = ExpandNumbers(text).ToUpper();

            string[] words = text.Split();
            for (int i = 0; i < words.Length; i++)
            {
                output += DecodeWord(words[i]);
            }
            return output;
        }

        //Decode the word into phenomes by looking for the longest word in the dictionary that matches
        //the first part of the word and so on. 
        //This works fairly well but could be improved. The original paper had a model that
        //dealt with guessing the phonemes of words
        public string DecodeWord(string word)
        {
            string output = "";
            int start = 0;
            for (int end = word.Length; end >= 0 && start < word.Length; end--)
            {
                if (end <= start) //no matches
                {
                    start++;
                    end = word.Length + 1;
                    continue;
                }
                string subword = word.Substring(start, end - start);
                if (dict.TryGetValue(subword, out string value))
                {
                    output += value + " ";
                    start = end;
                    end = word.Length + 1;
                }
            }
            return output;
        }

        int[] GetTokens(string ptext)
        {
            string[] p = ptext.Split();
            var tokens = new int[p.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = Mathf.Max(0, System.Array.IndexOf(phonemes, p[i]));
            }
            return tokens;
        }

        public void DoInference(string ptext)
        {
            if (hasPendingInference) return;

            int[] tokens = GetTokens(ptext);

            var input = new Tensor<int>(new TensorShape(tokens.Length), tokens);

            try
            {
                CreateWorker();
                worker.Schedule(input);

                outputTensor = worker.PeekOutput("wav") as Tensor<float>;

                outputTensor.ReadbackRequest();
                hasPendingInference = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"There has been an error when doing an Inference: {ex}");
                ProcessCancelled?.Invoke();
            }
            finally
            {
                input.Dispose();
            }
        }

        private void CompleteInference(float[] data)
        {
            var duration = data.Length / SampleRate;
            Debug.Log($"Audio size = {duration} seconds");

            clip = AudioClip.Create($"TTSAudio_{generatedSpeechCount}", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);

            audioSource.Stop();
            audioSource.clip = null;

            Speak();
            ProcessCompleted?.Invoke(duration);
        }

        private void Speak()
        {
            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                isPlayingSpeech = true;
            }
            else
            {
                Debug.LogError("There is no audio source");
            }
        }

        private void OnDestroy()
        {
            worker?.Dispose();
            outputTensor?.Dispose();
        }
    }
}
