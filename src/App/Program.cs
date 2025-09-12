using ScottPlot;

// Baby Born Toy Bathtub Selection Visualizer
// Hard-coded dimensions (centimeters)
// Shower box: outer 84x81, outer ring 78x75 (rounded), inner ring 60x60 (rounded)
// Bathtub: rounded-rectangle 28x51, rotated 90° and aligned to inner ring's bottom-left

// Assumptions:
// - Coordinates are in centimeters.
// - Corner radius is assumed to be 8% of the smaller side for rounded rectangles unless otherwise specified.

var plot = new Plot();

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
double tubW = 28, tubH = 51; // given dimensions
double tubRadius = Math.Min(tubW, tubH) * 0.12; // slightly larger rounding for tub
double innerHalfW = ringInnerW / 2.0;
double innerHalfH = ringInnerH / 2.0;

// No rotation for final orientation (short side horizontal)
double tubRotW = tubW;
double tubRotH = tubH;

// Align lower-left corner of rotated tub to inner ring's lower-left corner (-innerHalfW, -innerHalfH)
double tubCenterX = -innerHalfW + tubRotW / 2.0;
double tubCenterY = -innerHalfH + tubRotH / 2.0;

var tubPts = RoundedRectPolygon(tubCenterX, tubCenterY, tubW, tubH, tubRadius, rotationDegrees: 0, cornerSegments: 24);
var tubPoly = plot.Add.Polygon(tubPts);
tubPoly.FillColor = tubFill;
tubPoly.LineColor = tubBorder;
tubPoly.LineWidth = 3;
tubPoly.LegendText = "Bathtub 28×51 cm (short side horizontal)";

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

// Save PNG to the output directory with UTC timestamp
string outDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "output");
System.IO.Directory.CreateDirectory(outDir);
string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss'Z'");
string fileName = $"shower-bathtub_{stamp}.png";
string outPath = System.IO.Path.Combine(outDir, fileName);
plot.SavePng(outPath, 1200, 900);

Console.WriteLine($"Saved plot to {outPath}");
