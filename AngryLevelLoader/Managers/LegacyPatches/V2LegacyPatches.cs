using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public static class V2LegacyPatches
	{
		public static string catalogPath;
	}

	public static class V2LegacyAudioPatches
	{
		private static AudioMixerGroup allAudio;
		private static AudioMixerGroup musicAudio;
		private static AudioMixerGroup goreAudio;
		private static AudioMixerGroup doorAudio;
		private static AudioMixerGroup unfreezableAudio;

		internal static void Init()
		{
			V2LegacyPatches.catalogPath = Path.Combine(Plugin.workingDir, "V2LegacyBundle");

			allAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("AllAudio").WaitForCompletion();
			musicAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("MusicAudio").WaitForCompletion();
			goreAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("GoreAudio").WaitForCompletion();
			doorAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("DoorAudio").WaitForCompletion();
			unfreezableAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("UnfreezeableAudio").WaitForCompletion();

			string addressablePath = Path.Combine(Addressables.RuntimePath, "StandaloneWindows64");

			if (!File.Exists(Path.Combine(addressablePath, "other_assets_allaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_allaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_allaudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_dooraudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_dooraudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_dooraudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_goreaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_goreaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_goreaudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_musicaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_musicaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_musicaudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_unfreezeableaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_unfreezeableaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_unfreezeableaudio.bundle"));

			Addressables.LoadContentCatalogAsync(Path.Combine(Plugin.workingDir, "V2LegacyBundle", "catalog.json")).WaitForCompletion();
			GameObject mixerContainer = Addressables.LoadAssetAsync<GameObject>("LegacyV2Patches/AudioMixerContainer").WaitForCompletion();
			foreach (AudioSource src in mixerContainer.GetComponents<AudioSource>())
			{
				if (src.outputAudioMixerGroup == null || src.outputAudioMixerGroup.audioMixer == null)
					continue;

				string mixerName = src.outputAudioMixerGroup.audioMixer.name;

				AudioMixerGroup toLink = null;
				if (mixerName == "AllAudio")
					toLink = allAudio;
				else if (mixerName == "GoreAudio")
					toLink = goreAudio;
				else if (mixerName == "UnfreezeableAudio")
					toLink = unfreezableAudio;
				else if (mixerName == "MusicAudio")
					toLink = musicAudio;
				else if (mixerName == "DoorAudio")
					toLink = doorAudio;
				else
					continue;

				src.outputAudioMixerGroup.audioMixer.outputAudioMixerGroup = toLink;
			}
		}
	}
}
