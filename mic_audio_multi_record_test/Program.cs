using System.Diagnostics;
using System;
using System.Collections;
using System.Drawing;
using Microsoft.VisualBasic;
using System.Data;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Threading.Tasks;
using System.Threading;

namespace mic_audio_multi_record_test
{
    class Program
    {

        static readonly string _saveDirectory = @"C:\audio_recordings\";

        public static void Main()
        {
            int waveInDevices = WaveIn.DeviceCount;

            Console.WriteLine("------------ Available audio devices ------------");
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
            Console.WriteLine("-------------------------------------------------\n");

            Console.Write("Enter the device number you would like to record from, separated by spaces.\nDevice numbers: ");
            var chosenDevices = Console.ReadLine();

            List<Task> recordingTasks = new List<Task>();
            List<CancellationTokenSource> taskCancellations = new List<CancellationTokenSource>();

            System.IO.Directory.CreateDirectory(_saveDirectory);

            foreach (string index in chosenDevices.Split(' '))
            {
                CancellationTokenSource source = new CancellationTokenSource();
                taskCancellations.Add(source);
                recordingTasks.Add(recordAsync(Convert.ToInt32(index), source.Token));
            }

            foreach(Task t in recordingTasks)
            {
                t.Start();
            }

            Console.Write("\n-------------------\nPress enter to stop recording...\n-------------------");
            Console.ReadLine();

            foreach(CancellationTokenSource source in taskCancellations)
            {
                source.Cancel();
            }

            Task.WaitAll(recordingTasks.ToArray());
        }


        static Task recordAsync(int deviceNum, CancellationToken cancellationToken)
        {
            return new Task(async () =>
            {
                WaveFileWriter waveFile;
                WaveInEvent waveSource = new WaveInEvent();
                waveSource.DeviceNumber = deviceNum;
                waveSource.WaveFormat = new WaveFormat(44100, 1);

                string tempFile = ($@"{_saveDirectory}mic_recording_{deviceNum}.wav");
                waveFile = new WaveFileWriter(tempFile, waveSource.WaveFormat);

                waveSource.DataAvailable += (sender, e) =>
                {
                    waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                };

                waveSource.StartRecording();

                while(!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100);
                }

                waveSource.StopRecording();
                waveFile.Dispose();
            });
        }
    }
}
