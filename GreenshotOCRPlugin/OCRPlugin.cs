﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenshotOCR
{
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	[Export(typeof(IGreenshotPlugin))]
	public class OcrPlugin : IGreenshotPlugin
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (OcrPlugin));
		private string _ocrCommand;
		private static IOCRConfiguration _config;
		private PluginAttribute _myAttributes;
		private System.Windows.Forms.ToolStripMenuItem _ocrMenuItem = new System.Windows.Forms.ToolStripMenuItem();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_ocrMenuItem != null)
				{
					_ocrMenuItem.Dispose();
					_ocrMenuItem = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new OCRDestination(_ocrCommand);
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="greenshotHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttribute, CancellationToken token = new CancellationToken())
		{
			LOG.Debug("Initialize called of " + myAttribute.Name);

			_myAttributes = myAttribute;

			var dllPath = Path.GetDirectoryName(myAttribute.DllFile);

			if (dllPath != null)
			{
				_ocrCommand = Path.Combine(dllPath, "greenshotocrcommand.exe");
			}

			if (!HasModi())
			{
				LOG.Warn("No MODI found!");
				return false;
			}
			// Register / get the ocr configuration
			_config = await IniConfig.Current.RegisterAndGetAsync<IOCRConfiguration>(token);

			if (_config.Language != null)
			{
				_config.Language = _config.Language.Replace("miLANG_", "").Replace("_", " ");
			}
			return true;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown()
		{
			LOG.DebugFormat("Shutdown of {0}", _myAttributes.Name);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			if (!HasModi())
			{
				MessageBox.Show("Greenshot OCR", "Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			var settingsForm = new SettingsForm(Enum.GetNames(typeof (ModiLanguage)), _config);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				// "Re"set hotkeys
			}
		}

		/// <summary>
		/// Check if MODI is installed and available
		/// </summary>
		/// <returns></returns>
		private bool HasModi()
		{
			try
			{
				using (var process = Process.Start(_ocrCommand, "-c"))
				{
					if (process != null)
					{
						// TODO: Can change to async...
						process.WaitForExit();
						return process.ExitCode == 0;
					}
				}
			}
			catch (Exception e)
			{
				LOG.DebugFormat("Error trying to initiate MODI: {0}", e.Message);
			}
			LOG.InfoFormat("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}