using ScottPlot;
using ScottPlot.Blazor;

namespace DistributedRendering.AME.Frontend.Components.Widgets;

public partial class PlotTesting
{
	private BlazorPlot BlazorPlot { get; set; } = new();
	private FileInfo PlotImage { get; set; } = new("wwwroot/plot.webp");
	private string ImageSourceUrlString { get; set; } = "";

	private void OnClick()
	{
		if (PlotImage is { Exists: true })
		{
			ImageSourceUrlString = PlotImage.Name;
			this.StateHasChanged();

			return;
		}

		BlazorPlot.Plot.Add.Bars(
			new[]
			{
				new Bar
				{
					Position = 1D,
					Value = 5D
				},
				new Bar
				{
					Position = 3D,
					Value = 10D
				}
			}
		);

		BlazorPlot.Plot.SaveWebp(PlotImage.FullName, 500, 300);
		PlotImage.Refresh();
		ImageSourceUrlString = PlotImage.Name;

		this.StateHasChanged();
	}
}