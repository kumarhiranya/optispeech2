//	Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734
//
// Modified by the Optispeech 2 developers to take in the absolute filepath

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Optispeech.Sweeps {

	/// <summary>
	/// Saves wav data from an <see cref="AudioClip"/> to file
	/// </summary>
	public static class SavWav {

		/// <summary>
		/// Size of the header in a wav file
		/// </summary>
		const int HEADER_SIZE = 44;

		/// <summary>
		/// Saves the data from an audioclip to a file
		/// </summary>
		/// <param name="filepath">The file location to write the audio data to</param>
		/// <param name="clip">The audioclip that contains the data to write</param>
		/// <returns>True if the file is successfully written</returns>
		public static bool Save(string filepath, AudioClip clip) {
			if (!filepath.ToLower().EndsWith(".wav")) {
				filepath += ".wav";
			}

			// Make sure directory exists if user is saving to sub dir.
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));

			using (var fileStream = CreateEmpty(filepath)) {

				ConvertAndWrite(fileStream, clip);

				WriteHeader(fileStream, clip);
			}

			return true; // TODO: return false if there's a failure saving the file
		}

		/// <summary>
		/// Takes an audioclip and trims the beginning and end for as long as they're below a given volume threshold
		/// </summary>
		/// <param name="clip">The audioclip to trim</param>
		/// <param name="min">The minimum volume threshold that won't get filtered</param>
		/// <returns>A new audio clip with trimmed silence from either end</returns>
		/// <seealso cref="TrimSilence(List{float}, float, int, int)"/>
		/// <seealso cref="TrimSilence(List{float}, float, int, int, bool)"/>
		public static AudioClip TrimSilence(AudioClip clip, float min) {
			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

			return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
		}

		/// <summary>
		/// Calls <see cref="TrimSilence(List{float}, float, int, int, bool)"/> with streaming set to false
		/// </summary>
		/// <param name="samples">The audio samples to create an audio clip out of</param>
		/// <param name="min">The minimum volume threshold that won't get filtered out</param>
		/// <param name="channels">The number of channels to give the audioclip</param>
		/// <param name="hz">The frequency to give the audioclip</param>
		/// <returns>An audioclip with trimmed silence with the given samples, channels, and frequency</returns>
		/// <seealso cref="TrimSilence(AudioClip, float)"/>
		/// <seealso cref="TrimSilence(List{float}, float, int, int, bool)"/>
		public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz) {
			return TrimSilence(samples, min, channels, hz, false);
		}

		/// <summary>
		/// Creates an audio clip, but with the silence trimmed from both ends, from a given list of audio samples and volume threshold
		/// to determine what constitutes silent.
		/// </summary>
		/// <param name="samples">The audio samples to create an audio clip out of</param>
		/// <param name="min">The minimum volume threshold that won't get filtered out</param>
		/// <param name="channels">The number of channels to give the audioclip</param>
		/// <param name="hz">The frequency to give the audioclip</param>
		/// <param name="stream">The stream value to give the audioclip</param>
		/// <returns>An audioclip with trimmed silence with the given samples, channels, frequency, and stream values</returns>
		/// <seealso cref="TrimSilence(AudioClip, float)"/>
		/// <seealso cref="TrimSilence(List{float}, float, int, int)"/>
		public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool stream) {
			int i;

			for (i = 0; i < samples.Count; i++) {
				if (Mathf.Abs(samples[i]) > min) {
					break;
				}
			}

			samples.RemoveRange(0, i);

			for (i = samples.Count - 1; i > 0; i--) {
				if (Mathf.Abs(samples[i]) > min) {
					break;
				}
			}

			samples.RemoveRange(i, samples.Count - i);

			var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, stream);

			clip.SetData(samples.ToArray(), 0);

			return clip;
		}

		/// <summary>
		/// Create a <see cref="FileStream"/> to the given filepath with a header filled with empty bytes
		/// </summary>
		/// <param name="filepath">Filepath to create the <see cref="FileStream"/> at</param>
		/// <returns>The file stream with empty header</returns>
		static FileStream CreateEmpty(string filepath) {
			var fileStream = new FileStream(filepath, FileMode.Create);
			byte emptyByte = new byte();

			for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
			{
				fileStream.WriteByte(emptyByte);
			}

			return fileStream;
		}

		/// <summary>
		/// Converts the audio data from an <see cref="AudioClip"/> and writes it to a <see	cref="FileStream"/>
		/// </summary>
		/// <param name="fileStream">The file stream to write audio data to</param>
		/// <param name="clip">The audio clip to get data from</param>
		static void ConvertAndWrite(FileStream fileStream, AudioClip clip) {

			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

			Int16[] intData = new Int16[samples.Length];
			//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

			Byte[] bytesData = new Byte[samples.Length * 2];
			//bytesData array is twice the size of
			//dataSource array because a float converted in Int16 is 2 bytes.

			int rescaleFactor = 32767; //to convert float to Int16

			for (int i = 0; i < samples.Length; i++) {
				intData[i] = (short)(samples[i] * rescaleFactor);
				Byte[] byteArr = new Byte[2];
				byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}

			fileStream.Write(bytesData, 0, bytesData.Length);
		}

		/// <summary>
		/// Fills a <see cref="FileStream"/>'s header with data from an <see cref="AudioClip"/>
		/// </summary>
		/// <param name="fileStream">File stream to write the header to</param>
		/// <param name="clip">The audio clip to get header data from</param>
		static void WriteHeader(FileStream fileStream, AudioClip clip) {

			var hz = clip.frequency;
			var channels = clip.channels;
			var samples = clip.samples;

			fileStream.Seek(0, SeekOrigin.Begin);

			Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			fileStream.Write(riff, 0, 4);

			Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
			fileStream.Write(chunkSize, 0, 4);

			Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			fileStream.Write(wave, 0, 4);

			Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			fileStream.Write(fmt, 0, 4);

			Byte[] subChunk1 = BitConverter.GetBytes(16);
			fileStream.Write(subChunk1, 0, 4);

			//UInt16 two = 2;
			UInt16 one = 1;

			Byte[] audioFormat = BitConverter.GetBytes(one);
			fileStream.Write(audioFormat, 0, 2);

			Byte[] numChannels = BitConverter.GetBytes(channels);
			fileStream.Write(numChannels, 0, 2);

			Byte[] sampleRate = BitConverter.GetBytes(hz);
			fileStream.Write(sampleRate, 0, 4);

			Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
			fileStream.Write(byteRate, 0, 4);

			UInt16 blockAlign = (ushort)(channels * 2);
			fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

			UInt16 bps = 16;
			Byte[] bitsPerSample = BitConverter.GetBytes(bps);
			fileStream.Write(bitsPerSample, 0, 2);

			Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
			fileStream.Write(datastring, 0, 4);

			Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
			fileStream.Write(subChunk2, 0, 4);

			//		fileStream.Close();
		}
	}
}
