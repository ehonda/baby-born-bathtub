using ScottPlot;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;
using SPColor = ScottPlot.Color;
using SPColors = ScottPlot.Colors;

// Baby Born Toy Bathtub Selection Visualizer
// Hard-coded dimensions (centimeters)
// Shower box: outer 84x81, outer ring 78x75 (rounded), inner ring 60x60 (rounded)
// Bathtub: rounded-rectangle 28x51, rotated 90° and aligned to inner ring's bottom-left

// Assumptions:
// - Coordinates are in centimeters.
// - Corner radius is assumed to be 8% of the smaller side for rounded rectangles unless otherwise specified.

// CLI Settings
public class SinglePlotSettings : CommandSettings
{
	[CommandArgument(0, "[file]")]
	[Description("Path to bathtub model JSON (default: input/kaufland-heless.json)")]
	public string? FilePath { get; set; }
}

public class MultiPlotSettings : CommandSettings
{
	[CommandOption("-f|--file <FILE>")]
	[Description("Input JSON file(s). Specify multiple times for multiple models.")]
	public string[] Files { get; init; } = Array.Empty<string>();

	[CommandOption("-o|--out <PATH>")]
	[Description("Output PNG path (default: output/comparison.png)")]
	public string? OutPath { get; init; }

	[CommandOption("--cols <N>")]
	[Description("Number of columns in the grid (default: auto square-ish)")]
	public int? Cols { get; init; }

	[CommandOption("--tile-width <PX>")]
	[Description("Width in pixels per tile (default: 1400)")]
	public int? TileWidth { get; init; }

	[CommandOption("--tile-height <PX>")]
	[Description("Height in pixels per tile (default: 1000)")]
	public int? TileHeight { get; init; }
}

// Data Model
public record BathtubModel(
	string Name,
	double WidthCm,
	double HeightCm,
	double CornerRadiusPercent
);

// Commands
public sealed class SinglePlotCommand : Command<SinglePlotSettings>
{
	public override int Execute(CommandContext context, SinglePlotSettings settings)
	{
		try
		{
			string filePath = settings.FilePath ?? System.IO.Path.Combine("input", "kaufland-heless.json");
			if (!System.IO.File.Exists(filePath))
			{
				AnsiConsole.MarkupLine($"[red]Input file not found:[/] {filePath}");
				return -1;
			}

			BathtubModel? model = ModelLoader.LoadModel(filePath);
			if (model is null)
				return -2;

			string outDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "output");
			System.IO.Directory.CreateDirectory(outDir);
			string baseName = System.IO.Path.GetFileNameWithoutExtension(filePath);
			string outPath = System.IO.Path.Combine(outDir, baseName + ".png");

			Plotter.GeneratePlot(model, outPath, 1200, 900);
			AnsiConsole.MarkupLine($"Saved plot to [green]{outPath}[/]");
			return 0;
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
			return -3;
		}
	}
}

public sealed class MultiPlotCommand : Command<MultiPlotSettings>
{
	public override int Execute(CommandContext context, MultiPlotSettings settings)
	{
		try
		{
			var files = (settings.Files?.Length > 0 ? settings.Files : Array.Empty<string>()).ToList();
			if (files.Count == 0)
			{
				AnsiConsole.MarkupLine("[red]No input files provided. Use -f multiple times to add files.[/]");
				return -1;
			}

			// Load models and remember their source paths
			var items = new List<(string path, BathtubModel model)>();
			foreach (var f in files)
			{
				if (!System.IO.File.Exists(f))
				{
					AnsiConsole.MarkupLine($"[yellow]Skipping missing file:[/] {f}");
					continue;
				}
				var model = ModelLoader.LoadModel(f);
				if (model is null)
				{
					AnsiConsole.MarkupLine($"[yellow]Skipping invalid JSON:[/] {f}");
					continue;
				}
				items.Add((f, model));
			}

			if (items.Count == 0)
			{
				AnsiConsole.MarkupLine("[red]No valid inputs to process.[/]");
				return -2;
			}

			// Prepare output
			string outDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "output");
			System.IO.Directory.CreateDirectory(outDir);
			string outPath = settings.OutPath ?? System.IO.Path.Combine(outDir, "comparison.png");

			int tileW = settings.TileWidth ?? 1400;
			int tileH = settings.TileHeight ?? 1000;
			int n = items.Count;
			int cols = settings.Cols.HasValue && settings.Cols.Value > 0 ? settings.Cols.Value : (int)Math.Ceiling(Math.Sqrt(n));
			int rows = (int)Math.Ceiling(n / (double)cols);
			int margin = 40; // px outer margin
			int gutter = 30; // px spacing between tiles

			// Create a ScottPlot Multiplot and populate subplots
			var mp = new ScottPlot.Multiplot();
			mp.AddPlots(n);
			mp.Layout = new ScottPlot.MultiplotLayouts.Grid(rows: rows, columns: cols);

			for (int i = 0; i < n; i++)
			{
				Plot plot = mp.GetPlot(i);
				var model = items[i].model;
				Plotter.FillPlot(plot, model);
			}

			int totalW = cols * tileW + (cols - 1) * gutter + 2 * margin;
			int totalH = rows * tileH + (rows - 1) * gutter + 2 * margin;

			// Note: Multiplot handles layout; explicit margins/gutters are approximated by choosing larger canvas
			mp.SavePng(outPath, totalW, totalH);
			AnsiConsole.MarkupLine($"Saved comparison to [green]{outPath}[/]");

			return 0;
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
			return -3;
		}
	}
}

// Entry
internal class Program
{
	public static int Main(string[] args)
	{
		var app = new CommandApp<SinglePlotCommand>();
		app.Configure(cfg =>
		{
			cfg.AddCommand<SinglePlotCommand>("single");
			cfg.AddCommand<MultiPlotCommand>("multi");
			// Default command is SinglePlotCommand via generic CommandApp
		});
		return app.Run(args);
	}
}
// Entry is defined below with multi-command support

// ----------------------------
// Plotting implementation
// ----------------------------

public static class Plotter
{
	public static void GeneratePlot(BathtubModel model, string outPath, int width = 1200, int height = 900)
	{
		var plot = new Plot();
		FillPlot(plot, model);
		plot.SavePng(outPath, width, height);
	}

	public static void FillPlot(Plot plot, BathtubModel model)
	{
		// Geometry helpers
		static Coordinates RotatePoint(Coordinates p, Coordinates center, double radians)
		{
			double cos = Math.Cos(radians);
			double sin = Math.Sin(radians);
			double dx = p.X - center.X;
			double dy = p.Y - center.Y;
			return new Coordinates(
				center.X + dx * cos - dy * sin,
				center.Y + dx * sin + dy * cos
			);
		}

		static Coordinates[] RoundedRectPolygon(double centerX, double centerY, double width, double height, double cornerRadius,
			double rotationDegrees = 0, int cornerSegments = 16)
		{
			// Clamp radius
			double maxR = Math.Min(width, height) / 2.0;
			double r = Math.Clamp(cornerRadius, 0, maxR);

			double hx = width / 2.0;
			double hy = height / 2.0;

			// Corner centers (axis-aligned, before rotation), clockwise starting at top-right
			var ctrTR = new Coordinates(centerX + hx - r, centerY + hy - r);
			var ctrBR = new Coordinates(centerX + hx - r, centerY - hy + r);
			var ctrBL = new Coordinates(centerX - hx + r, centerY - hy + r);
			var ctrTL = new Coordinates(centerX - hx + r, centerY + hy - r);

			List<Coordinates> pts = new();

			// Helper to add quarter-arc points
			void AddArc(Coordinates c, double startDeg, double endDeg)
			{
				int steps = Math.Max(1, cornerSegments);
				double startRad = startDeg * Math.PI / 180.0;
				double endRad = endDeg * Math.PI / 180.0;
				for (int i = 0; i <= steps; i++)
				{
					double t = i / (double)steps;
					double ang = startRad + t * (endRad - startRad);
					pts.Add(new Coordinates(c.X + r * Math.Cos(ang), c.Y + r * Math.Sin(ang)));
				}
			}

			// Build a clockwise path starting at the top-left arc and proceed around edges
			// Top-left arc: 180° (west) to 90° (north)
			AddArc(ctrTL, 180, 90);
			// Top-right arc: 90° (north) to 0° (east)
			AddArc(ctrTR, 90, 0);
			// Bottom-right arc: 0° (east) to -90° (south)
			AddArc(ctrBR, 0, -90);
			// Bottom-left arc: -90° (south) to -180° (west)
			AddArc(ctrBL, -90, -180);

			// Apply rotation if needed
			if (rotationDegrees != 0)
			{
				double rad = rotationDegrees * Math.PI / 180.0;
				var center = new Coordinates(centerX, centerY);
				for (int i = 0; i < pts.Count; i++)
					pts[i] = RotatePoint(pts[i], center, rad);
			}

			return pts.ToArray();
		}

		// Helper to add a centered rectangle with size w x h
		void AddCenteredRect(double w, double h, SPColor fill, SPColor border, double borderWidth, string? label = null)
		{
			// Place rectangle centered at origin for a top-down footprint
			double xMin = -w / 2.0;
			double xMax = +w / 2.0;
			double yMin = -h / 2.0;
			double yMax = +h / 2.0;
			var r = plot.Add.Rectangle(xMin, xMax, yMin, yMax);
			r.FillStyle.Color = fill;
			r.LineStyle.Color = border;
			r.LineStyle.Width = (float)borderWidth;
			if (!string.IsNullOrWhiteSpace(label))
				r.LegendText = label;
		}

		// Colors
		SPColor showerOuterFill = SPColors.LightGray.WithAlpha(.15);
		SPColor showerRingFill = SPColors.SlateGray.WithAlpha(.10);
		SPColor showerInnerFill = SPColors.SteelBlue.WithAlpha(.07);
		SPColor showerBorder = SPColors.DarkSlateGray;
		SPColor tubFill = SPColors.Orange.WithAlpha(.25);
		SPColor tubBorder = SPColors.DarkOrange;

		// Add shower outer rectangle (square corners)
		AddCenteredRect(84, 81, showerOuterFill, showerBorder, 2, label: "Shower Outer 84×81 cm");

		// Add shower rounded rings
		double ringOuterW = 78, ringOuterH = 75;
		double ringInnerW = 60, ringInnerH = 60;
		double ringOuterR = Math.Min(ringOuterW, ringOuterH) * 0.08; // 8% corner radius assumption
		double ringInnerR = Math.Min(ringInnerW, ringInnerH) * 0.08;

		var outerRingPts = RoundedRectPolygon(0, 0, ringOuterW, ringOuterH, ringOuterR, rotationDegrees: 0, cornerSegments: 24);
		var outerRing = plot.Add.Polygon(outerRingPts);
		outerRing.FillColor = showerRingFill;
		outerRing.LineColor = showerBorder;
		outerRing.LineWidth = 2;
		outerRing.LegendText = "Outer Ring 78×75 cm";

		var innerRingPts = RoundedRectPolygon(0, 0, ringInnerW, ringInnerH, ringInnerR, rotationDegrees: 0, cornerSegments: 24);
		var innerRing = plot.Add.Polygon(innerRingPts);
		innerRing.FillColor = showerInnerFill;
		innerRing.LineColor = showerBorder;
		innerRing.LineWidth = 2;
		innerRing.LegendText = "Inner Ring 60×60 cm";

		// Add bathtub as rounded rectangle with short side parallel to X axis, aligned to bottom-left of inner ring
		double tubW = model.WidthCm;
		double tubH = model.HeightCm;
		double tubRadius = Math.Min(tubW, tubH) * (model.CornerRadiusPercent / 100.0);
		double innerHalfW = ringInnerW / 2.0;
		double innerHalfH = ringInnerH / 2.0;

		// No rotation for final orientation (short side horizontal)
		double tubRotW = tubW;
		double tubRotH = tubH;

		// Align lower-left corner of rotated tub to inner ring's lower-left corner (-innerHalfW, -innerHalfH)
		double tubCenterX = -innerHalfW + tubRotW / 2.0;
		// Align the bathtub's half-depth (midline) with the inner ring's midline (y=0)
		double tubCenterY = 0;

		var tubPts = RoundedRectPolygon(tubCenterX, tubCenterY, tubW, tubH, tubRadius, rotationDegrees: 0, cornerSegments: 24);
		var tubPoly = plot.Add.Polygon(tubPts);
		tubPoly.FillColor = tubFill;
		tubPoly.LineColor = tubBorder;
		tubPoly.LineWidth = 3;
		tubPoly.LegendText = $"Bathtub {tubW}×{tubH} cm";

		// Axis setup: square units so geometry isn't distorted
		plot.Axes.SquareUnits();

		// Tight margins leaving a little padding for visibility
		plot.Axes.Margins(0.05, 0.05);

		// Set limits so everything fits comfortably
		double pad = 6; // cm padding around the largest footprint
		plot.Axes.SetLimits(-84 / 2.0 - pad, 84 / 2.0 + pad, -81 / 2.0 - pad, 81 / 2.0 + pad);

		// Add a legend for clarity
		plot.Legend.IsVisible = true;

		// Title and axis labels (units)
		plot.Title(string.IsNullOrWhiteSpace(model.Name) ? "Bathtub Model" : model.Name);
		plot.Axes.Bottom.Label.Text = "Width (cm)";
		plot.Axes.Left.Label.Text = "Depth (cm)";
	}

	private static Coordinates RotatePoint(Coordinates p, Coordinates center, double radians)
	{
		double cos = Math.Cos(radians);
		double sin = Math.Sin(radians);
		double dx = p.X - center.X;
		double dy = p.Y - center.Y;
		return new Coordinates(
			center.X + dx * cos - dy * sin,
			center.Y + dx * sin + dy * cos
		);
	}

	private static Coordinates[] RoundedRectPolygon(double centerX, double centerY, double width, double height, double cornerRadius,
		double rotationDegrees = 0, int cornerSegments = 16)
	{
		double maxR = Math.Min(width, height) / 2.0;
		double r = Math.Clamp(cornerRadius, 0, maxR);

		double hx = width / 2.0;
		double hy = height / 2.0;

		var ctrTR = new Coordinates(centerX + hx - r, centerY + hy - r);
		var ctrBR = new Coordinates(centerX + hx - r, centerY - hy + r);
		var ctrBL = new Coordinates(centerX - hx + r, centerY - hy + r);
		var ctrTL = new Coordinates(centerX - hx + r, centerY + hy - r);

		List<Coordinates> pts = new();

		void AddArc(Coordinates c, double startDeg, double endDeg)
		{
			int steps = Math.Max(1, cornerSegments);
			double startRad = startDeg * Math.PI / 180.0;
			double endRad = endDeg * Math.PI / 180.0;
			for (int i = 0; i <= steps; i++)
			{
				double t = i / (double)steps;
				double ang = startRad + t * (endRad - startRad);
				pts.Add(new Coordinates(c.X + r * Math.Cos(ang), c.Y + r * Math.Sin(ang)));
			}
		}

		// Clockwise path starting at top-left corner
		AddArc(ctrTL, 180, 90);
		AddArc(ctrTR, 90, 0);
		AddArc(ctrBR, 0, -90);
		AddArc(ctrBL, -90, -180);

		if (rotationDegrees != 0)
		{
			double rad = rotationDegrees * Math.PI / 180.0;
			var center = new Coordinates(centerX, centerY);
			for (int i = 0; i < pts.Count; i++)
				pts[i] = RotatePoint(pts[i], center, rad);
		}

		return pts.ToArray();
	}
}

// Helpers
internal static class ModelLoader
{
	public static BathtubModel? LoadModel(string filePath)
	{
		try
		{
			string json = System.IO.File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<BathtubModel>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
		}
		catch
		{
			AnsiConsole.MarkupLine($"[red]Failed to read or parse:[/] {filePath}");
			return null;
		}
	}
}
