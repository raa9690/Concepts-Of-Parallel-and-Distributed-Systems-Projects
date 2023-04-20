using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiningPhilosophers1
{
	/// <summary>
	/// Centralize the access to the configuration file.
	/// 
	/// In this simple application I have decided that the correct handling of erroneous configuration values
	/// is a replacement with default values.  In a different kind of an application this decision may be the
	/// wrong decision and having an exception bubble up and the program will not allow to continue...
	/// </summary>
	public class ConfigValue
	{
		private static readonly Lazy<ConfigValue> LazyInst = new Lazy<ConfigValue>(() => new ConfigValue());
		public static readonly ConfigValue Inst = LazyInst.Value;

		private readonly Dictionary<string, string> _appSettings;

		private ConfigValue()
		{
			_appSettings = ConfigurationManager.AppSettings.AllKeys.ToDictionary(x => x, x => ConfigurationManager.AppSettings[x]);
		}

		public int PhilosopherCount
		{
			get
			{
				const string key = "Philosopher Count";
				const int philosopherCountDefault = 5;
				var philosopherCount = ExtractInteger(key, philosopherCountDefault);
				return philosopherCount;
			}
		}

		public int ForkCount
		{
			get
			{
				const string key = "Fork Count";
				var forkCountDefault = PhilosopherCount;
				var forkCount = ExtractInteger(key, forkCountDefault);
				return forkCount;
			}
		}

		public int MaxPhilsophersToEatSimultaneously
		{
			get
			{
				const string key = "Max philosophers to eat simultaneously";
				const int maxPhilsophersToEatSimultaneouslyDefault = 2;
				var maxPhilsophersToEatSimultaneously = ExtractInteger(key, maxPhilsophersToEatSimultaneouslyDefault);
				return maxPhilsophersToEatSimultaneously;
			}
		}

		public int DurationPhilosophersEat
		{
			get
			{
				const string key = "Duration Allow Philosophers To Eat [seconds]";
				const int durationPhilosophersEatDefault = 20 * 1000;
				var durationPhilosophersEat = ExtractInteger(key, durationPhilosophersEatDefault);
				return durationPhilosophersEat * 1000;
			}
		}

		public int MaxThinkDuration
		{
			get
			{
				const string key = "philosopher Max Think Duration [milliseconds]";
				const int maxThinkDurationDefault = 1000;
				var maxThinkDuration = ExtractInteger(key, maxThinkDurationDefault);
				return maxThinkDuration;
			}
		}

		//<!-- Minimum think duration -->
		//<add key = "philosopher Min Think Duration [milliseconds]" value="50"/>
		public int MinThinkDuration
		{
			get
			{
				const string key = "philosopher Min Think Duration [milliseconds]";
				const int minThinkDurationDefault = 50;
				var minThinkDuration = ExtractInteger(key, minThinkDurationDefault);
				return minThinkDuration;
			}
		}

		public int DurationBeforeAskingPermissionToEat
		{
			get
			{
				const string key = "Duration Before Requesting Next Permission To Eat [milliseconds]";
				const int durationBeforeSeekEatPermissionDefault = 20;
				var durationBeforeSeekEatPermission = ExtractInteger(key, durationBeforeSeekEatPermissionDefault);
				return durationBeforeSeekEatPermission;
			}
		}

		//
		// Internal
		//
		// The regular expression pattern: @"\{\%(?<val>.*?)\%\}" searches for the characters:
		// "{" followed by "%" followed by any number of characters collectively named "val" 
		// followed by "%" and followed by "}"
		//
		// This will allow us to have in our configuration file:
		// 		<add key="Philosopher Count" value="5" />
		//		<add key="Fork Count" value="{%Philosopher Count%}" />
		//
		// and the "Fork Count" key will evaluate to the same value as "Philosopher Count".
		//
		// Note that the regular expression pattern: @"\{\%(?<val>.*?)\%\}", is not perfect.  
		// For example it will not handle nested "{%..%}" patterns  correctly (like, @"{%{%abc%}%}").
		// For as long as each key contains a non-nested "{%..%}" then we will be OK.
		// The keys may contain more than one "{%..%}" construct and the below 
		// GetConfigValue(..) will evaluate it correctly.
		//
		private readonly Regex _reValue = new Regex(@"\{\%(?<val>.*?)\%\}", RegexOptions.Singleline);

		private string GetConfigValue(string key)
		{
			var rawValue = _appSettings[key];
			if (string.IsNullOrEmpty(rawValue)) return string.Empty;

			for (; ; )
			{
				var m = _reValue.Match(rawValue);
				if (!m.Success) return rawValue;

				rawValue = _reValue.Replace(rawValue, ReplaceKey);
			}
		}

		private string ReplaceKey(Match m)
		{
			var key = m.Groups["val"].Value;
			if (string.IsNullOrEmpty(key)) return string.Empty;

			var rawValue = _appSettings[key];
			return rawValue ?? string.Empty;
		}

		private int ExtractInteger(string key, int defaultValue)
		{
			string sValue;
			try
			{
				sValue = GetConfigValue(key);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(new string('-', 100));
				return defaultValue;
			}

			var rc = int.TryParse(sValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out int intValue);
			if (!rc)
			{
				Console.WriteLine($"{key} configuration variable does not value to an integer: \"{sValue}\".  Using default {defaultValue}");
				return defaultValue;
			}

			if (intValue <= 0)
			{
				Console.WriteLine($"{key} configuration variable may be 0 or negative: {intValue}.  Using default {defaultValue}");
				return defaultValue;
			}

			return intValue;
		}
	}
}
