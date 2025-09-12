using ScottPlot;

// Baby Born Toy Bathtub Selection Visualizer
// Hard-coded dimensions (centimeters)
// Shower box: outer 84x81, outer ring 78x75, inner ring 60x60
// Bathtub: rounded-rectangle 28x51 (approximate as rectangle for now)

// Assumptions:
// - Coordinates are in centimeters.
// - We approximate rounded corners as simple rectangles. If needed later we can refine using polygons or arcs.

var plot = new Plot();

// Helper to add a centered rectangle with size w x h
void AddCenteredRect(double w, double h, Color fill, Color border, double borderWidth, string? label = null)
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
Color showerOuterFill = Colors.LightGray.WithAlpha(.15);
Color showerRingFill = Colors.SlateGray.WithAlpha(.10);
Color showerInnerFill = Colors.SteelBlue.WithAlpha(.07);
Color showerBorder = Colors.DarkSlateGray;
Color tubFill = Colors.Orange.WithAlpha(.25);
Color tubBorder = Colors.DarkOrange;

// Add shower rectangles (largest first so smaller ones draw on top)
AddCenteredRect(84, 81, showerOuterFill, showerBorder, 2, label: "Shower Outer 84×81 cm");
AddCenteredRect(78, 75, showerRingFill, showerBorder, 2, label: "Outer Ring 78×75 cm");
AddCenteredRect(60, 60, showerInnerFill, showerBorder, 2, label: "Inner Ring 60×60 cm");

// Add bathtub rectangle (approximate rounded edges by a rectangle). We'll place it centered too.
AddCenteredRect(51, 28, tubFill, tubBorder, 3, label: "Bathtub 51×28 cm");

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
plot.Title("Shower Box vs. Baby Born Bathtub (cm)");
plot.Axes.Bottom.Label.Text = "Width (cm)";
plot.Axes.Left.Label.Text = "Depth (cm)";

// Save PNG to the output directory
string fileName = "shower-bathtub.png";
plot.SavePng(fileName, 1000, 800);

Console.WriteLine($"Saved plot to {System.IO.Path.GetFullPath(fileName)}");
