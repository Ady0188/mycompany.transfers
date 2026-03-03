namespace MyCompany.Transfers.Admin.Client.Themes;

/// <summary>
/// Тема админ-панели: глубокий синий, тёмный сайдбар, светлый контент.
/// </summary>
public static class AdminTheme
{
    public static MudBlazor.MudTheme Theme { get; } = new()
    {
        PaletteLight = new MudBlazor.PaletteLight()
        {
            Primary = "#1565c0",
            PrimaryDarken = "#0d47a1",
            Secondary = "#00acc1",
            SecondaryDarken = "#00838f",
            AppbarBackground = "#1565c0",
            Background = "#f5f7fa",
            Surface = "#ffffff"
        },
        PaletteDark = new MudBlazor.PaletteDark()
        {
            Primary = "#42a5f5",
            Secondary = "#26c6da",
            AppbarBackground = "#0d2137",
            Background = "#0a1929",
            Surface = "#0d2137"
        },
        LayoutProperties = new MudBlazor.LayoutProperties()
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "240px",
            DrawerWidthRight = "320px"
        }
    };
}
