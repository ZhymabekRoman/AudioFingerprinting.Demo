using FFMpegCore;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Emy;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SoundFingerprinting.AddictedCS.Demo
{
    class Program
    {
        static IAudioService audioService = new FFmpegAudioService();
	static IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
        // static IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library

        static async Task Main(string[] args)
        {
	    var files = Directory.EnumerateFiles("./samples/", "*.mp3", SearchOption.AllDirectories);

            foreach (var filename in files)
	    {
		Console.WriteLine(filename);
	        await StoreForLaterRetrieval(filename);
            }
            var foundTrack = await GetBestMatchForSong("recorder.mp3");
            Console.WriteLine(foundTrack?.Id);
        }

        public static async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
	{
            var mediaInfo = await FFProbe.AnalyseAsync(queryAudioFile);

            int secondsToAnalyze = (int)mediaInfo.Duration.Seconds; // number of seconds to analyze from query file

            // int secondsToAnalyze = 3; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining

            // query the underlying database for similar audio sub-fingerprints
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .WithQueryConfig(new HighPrecisionQueryConfiguration())
                                                 .UsingServices(modelService, audioService)
                                                 .Query();

            return queryResult.BestMatch?.Track;
        }

        public static async Task StoreForLaterRetrieval(string pathToAudioFile)
        {
            var track = new TrackInfo(Path.GetFileNameWithoutExtension(pathToAudioFile), string.Empty, string.Empty);

            // create fingerprints
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .WithFingerprintConfig(new HighPrecisionFingerprintConfiguration())
                                        .UsingServices(audioService)
                                        .Hash();

            // store hashes in the database for later retrieval
            modelService.Insert(track, hashedFingerprints);
        }
    }
}
