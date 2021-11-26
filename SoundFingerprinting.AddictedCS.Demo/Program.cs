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
        static async Task Main(string[] args)
	{
	    if (args.Length < 3)
	    {
	        Console.WriteLine("Arguments count is < 3");
		System.Environment.Exit(1);
	    }

	    var audioService = new FFmpegAudioService();
	    var modelService = new InMemoryModelService(args[1]); // store fingerprints in RAM
	    
	    switch (args[0])
    	    {
                case "add":
                    await StoreForLaterRetrieval(args[2], modelService, audioService);
                    break;

                case "match":
                    var foundTrack = await GetBestMatchForSong(args[2], modelService, audioService);
		    if (foundTrack is null)
	            {
		        Console.WriteLine("Это божественная музыка! Возможно, именно поэтому я не могу найти её. 😇");
		    }
		    else
		    {
		        Console.WriteLine(foundTrack?.Id);
		    }
                    break;

                case "remove":
                    modelService.DeleteTrack(Path.GetFileNameWithoutExtension(args[2]));;
                    break;

                default:
                    Console.WriteLine($"Unknown command was passed: {args[0]}.");
                    break;
            }
	    // var files = Directory.EnumerateFiles("./samples/", "*.mp3", SearchOption.AllDirectories);
	    // foreach (var filename in files)
	    // {
		// Console.WriteLine(filename);
	        // await StoreForLaterRetrieval(filename, modelService, audioService);
            // }
	    
	    modelService.Snapshot(args[1]);
        }

        public static async Task<TrackData> GetBestMatchForSong(string queryAudioFile, InMemoryModelService modelService, FFmpegAudioService audioService)
	{
            var mediaInfo = await FFProbe.AnalyseAsync(queryAudioFile);

            int secondsToAnalyze = (int)mediaInfo.Duration.Seconds; // number of seconds to analyze from query file

            // int secondsToAnalyze = 3; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining

            // query the underlying database for similar audio sub-fingerprints
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(modelService, audioService)
                                                 .Query();

            return queryResult.BestMatch?.Track;
        }

        public static async Task StoreForLaterRetrieval(string pathToAudioFile, InMemoryModelService modelService, FFmpegAudioService audioService)
        {
            var track = new TrackInfo(Path.GetFileNameWithoutExtension(pathToAudioFile), string.Empty, string.Empty);

            // create fingerprints
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .UsingServices(audioService)
                                        .Hash();

            // store hashes in the database for later retrieval
            modelService.Insert(track, hashedFingerprints);
        }
    }
}
