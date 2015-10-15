/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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

using Dapplo.Addons;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenshotPicasaPlugin
{
	/// <summary>
	/// This is the Picasa base code
	/// </summary>
	[Plugin(Configurable=true)]
	[StartupAction]
	public class PicasaPlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (PicasaPlugin));
		private static IPicasaConfiguration config;
		private static IPicasaLanguage language;
		private ComponentResourceManager resources;
		private ToolStripMenuItem itemPlugInRoot;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (itemPlugInRoot != null)
				{
					itemPlugInRoot.Dispose();
					itemPlugInRoot = null;
				}
			}
		}

		[Import]
		public IGreenshotHost PluginHost
		{
			get;
			set;
		}

		public PicasaPlugin()
		{
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new PicasaDestination();
		}


		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Register / get the picasa configuration
			config = await IniConfig.Current.RegisterAndGetAsync<IPicasaConfiguration>();
			language = await LanguageLoader.Current.RegisterAndGetAsync<IPicasaLanguage>();
			resources = new ComponentResourceManager(typeof (PicasaPlugin));

			itemPlugInRoot = new ToolStripMenuItem();
			itemPlugInRoot.Text = language.Configure;
			itemPlugInRoot.Tag = PluginHost;
			itemPlugInRoot.Image = (Image) resources.GetObject("Picasa");
			itemPlugInRoot.Click += ConfigMenuClick;
			PluginUtils.AddToContextMenu(PluginHost, itemPlugInRoot);
			language.PropertyChanged += OnLanguageChanged;
		}

		public void OnLanguageChanged(object sender, EventArgs e)
		{
			if (itemPlugInRoot != null)
			{
				itemPlugInRoot.Text = language.Configure;
			}
		}

		public Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			LOG.Debug("Picasa Plugin shutdown.");
			language.PropertyChanged -= OnLanguageChanged;
			//host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
			return Task.FromResult(true);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			ShowConfigDialog();
		}


		/// <summary>
		/// A form for token
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog()
		{
			DialogResult result = new SettingsForm().ShowDialog();
			if (result == DialogResult.OK)
			{
				return true;
			}
			return false;
		}

		private void ConfigMenuClick(object sender, EventArgs eventArgs)
		{
			ShowConfigDialog();
		}
	}
}