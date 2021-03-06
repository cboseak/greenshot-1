/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Picasa.Forms;
using Dapplo.Addons;

namespace Greenshot.Addon.Picasa
{
	/// <summary>
	/// This is the Picasa base code
	/// </summary>
	[Plugin("Picasa", Configurable=true)]
	[StartupAction]
	public class PicasaPlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(PicasaPlugin));
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInRoot;

		[Import]
		private IPicasaConfiguration PicasaConfiguration
		{
			get;
			set;
		}

		[Import]
		private IPicasaLanguage PicasaLanguage
		{
			get;
			set;
		}

		[Import]
		private IGreenshotHost PluginHost
		{
			get;
			set;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_itemPlugInRoot != null)
				{
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		/// <summary>
		/// Implementation of the StartAsync
		/// </summary>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Register / get the picasa configuration
			_resources = new ComponentResourceManager(typeof (PicasaPlugin));

			_itemPlugInRoot = new ToolStripMenuItem
			{
				Text = PicasaLanguage.Configure, Tag = PluginHost, Image = (Image) _resources.GetObject("Picasa")
			};
			_itemPlugInRoot.Click += (sender, eventArgs) => Configure();
			PluginUtils.AddToContextMenu(PluginHost, _itemPlugInRoot);
			PicasaLanguage.PropertyChanged += OnPicasaLanguageChanged;
			return Task.FromResult(true);
		}

		public void OnPicasaLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInRoot != null)
			{
				_itemPlugInRoot.Text = PicasaLanguage.Configure;
			}
		}

		public Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			Log.Debug("Picasa Plugin shutdown.");
			PicasaLanguage.PropertyChanged -= OnPicasaLanguageChanged;
			//host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
			return Task.FromResult(true);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm().ShowDialog();
		}
	}
}