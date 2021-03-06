﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using System.Drawing.Printing;
using Greenshot.Helpers;
using System.Linq;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of PrinterDestination.
	/// </summary>
	[Destination(PrinterDesignation, 4)]
	public sealed class PrinterDestination : AbstractDestination
	{
		private const string PrinterDesignation = "Printer";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(PrinterDestination));
		private static readonly BitmapSource PrinterIcon;
		static PrinterDestination()
		{
			using (var printerIcon = GreenshotResources.GetImage("Printer.Image"))
			{
				PrinterIcon = printerIcon.ToBitmapSource();
			}
        }
		[Import]
		private ICoreConfiguration CoreConfiguration
		{
			get;
			set;
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		protected override void Initialize()
		{
			base.Initialize();
			Text = GreenshotLanguage.SettingsDestinationPrinter;
			Designation = PrinterDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null, token);
			Icon = PrinterIcon;
		}

		/// <summary>
		/// Load the current editors to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override async Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			Children.Clear();

			await Task.Run(
				() =>
				{
					var settings = new PrinterSettings();
					string defaultPrinter = settings.PrinterName;
					var printers = PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(x => x).ToList();
					var defaultIndex = printers.IndexOf(defaultPrinter);
					if (defaultIndex > 0)
					{
						printers.RemoveAt(defaultIndex);
						printers.Insert(0, defaultPrinter);
					}
					return printers.Select(printer => new PrinterDestination
					{
						Text = printer,
						Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, printer, exportToken),
						Icon = PrinterIcon,
						CoreConfiguration = CoreConfiguration,
						GreenshotLanguage = GreenshotLanguage
					}).ToList();
				}, token).ContinueWith(async printerDestinations =>
					{
						foreach (var printerDestination in await printerDestinations)
						{
							Children.Add(printerDestination);
						}

					}, token, TaskContinuationOptions.None, UiContext.UiTaskScheduler
				).ConfigureAwait(false);
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, string printerName, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = PrinterDesignation,
				SourceType = SourceTypes.Destination,
				Text = Text
			};

			try
			{
				await UiContext.RunOn(() =>
				{
					if (!string.IsNullOrEmpty(printerName))
					{
						using (var printHelper = new PrintHelper(capture, capture.CaptureDetails))
						{
							printHelper.PrintTo(printerName);
						}
					}
					else
					{
						using (var printHelper = new PrintHelper(capture, capture.CaptureDetails))
						{
							printHelper.PrintWithDialog();
						}
					}
				}, token);
			}
			catch (Exception e)
			{
				Log.Error(e, "Printer export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.PrintError;
            }

			return returnValue;
		}
	}
}