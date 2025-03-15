using System.ComponentModel;
using System.Text.Json;
using McpDotNet.Server;
using Microsoft.Playwright;
using PW = Microsoft.Playwright.Playwright;

namespace MCP.Playwright.Tools;

[McpToolType]
public static class PlaywrightTool
{
    [McpTool(PlaywrightToolNames.Navigate)]
    public static async Task<string> Navigate(
        string url,
        [Description("Navigation timeout in milliseconds")] int timeout = 20000)
    {
        var page = await BrowserProxy.EnsurePage();
        await page.GotoAsync(url, new PageGotoOptions { Timeout = timeout, WaitUntil = WaitUntilState.Load });

        return "Navigated to " + url;
    }

    [McpTool(PlaywrightToolNames.Screenshot)]
    [Description("Take a screenshot of the current page or a specific element")]
    public static async Task<string> Screenshot(
        [Description("Name for the screenshot")] string name,
        [Description("CSS selector for element to screenshot")] string? selector = null,
        [Description("Take a full page screenshot (default: false)")] bool fullPage = false,
        [Description("Save the screenshot as a PNG file (default: false)")] bool savePng = false,
        [Description("Directory to save the screenshot (default: user's Downloads folder)")] string? downloadsDir = null)
    {
        var page = await BrowserProxy.EnsurePage();

        downloadsDir ??= Path.Combine(downloadsDir ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{name}.png");
        if (!Directory.Exists(downloadsDir))
            Directory.CreateDirectory(downloadsDir);

        var filePath = Path.Combine(downloadsDir, $"{name}-{DateTime.Now.Ticks}.png");
        var fileType = savePng ? ScreenshotType.Png : ScreenshotType.Jpeg;

        if (!string.IsNullOrEmpty(selector))
        {
            var element = await page.QuerySelectorAsync(selector);
            if (element == null)
                return $"Element not found: {selector}";

            await element.ScrollIntoViewIfNeededAsync();
            await element.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Path = filePath,
                Type = fileType
            });
        }
        else
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filePath,
                FullPage = fullPage,
                Type = fileType,
            });
        }

        return "Screenshot saved to " + filePath;
    }

    [McpTool(PlaywrightToolNames.Click)]
    [Description("Click an element on the page")]
    public static async Task<string> Click(
        [Description("CSS selector for the element to click")] string selector)
    {
        var page = await BrowserProxy.EnsurePage();
        await page.ClickAsync(selector);

        return "Clicked element: " + selector;
    }

    [McpTool(PlaywrightToolNames.IframeClick)]
    [Description("Click an element in an iframe on the page")]
    public static async Task<string> IframeClick(
        [Description("CSS selector for the element to click")] string selector,
        [Description("CSS selector for the iframe containing the element to click")] string iframeSelector)
    {
        var page = await BrowserProxy.EnsurePage();
        var iframe = page.FrameLocator(iframeSelector);
        if (iframe == null)
            throw new InvalidOperationException($"Iframe not found: {iframeSelector}");

        await iframe.Locator(selector).ClickAsync();

        return "Clicked element: " + selector + " in iframe: " + iframeSelector;
    }

    [McpTool(PlaywrightToolNames.Fill)]
    [Description("Fill an input field with a given value")]
    public static async Task<string> Fill(
        [Description("CSS selector for the input field to fill")] string selector,
        [Description("Value to fill in the input field")] string value)
    {
        var page = await BrowserProxy.EnsurePage();
        await page.WaitForSelectorAsync(selector);
        await page.FillAsync(selector, value);

        return "Filled " + selector + " with: " + value;
    }

    [McpTool(PlaywrightToolNames.Select)]
    [Description("Select an element on the page with Select tag")]
    public static async Task<string> Select(
        [Description("CSS selector for element to select")] string selector,
        [Description("Value to select")] string value)
    {
        var page = await BrowserProxy.EnsurePage();
        await page.WaitForSelectorAsync(selector);
        await page.SelectOptionAsync(selector, value);

        return "Selected " + selector + " with: " + value;
    }

    [McpTool(PlaywrightToolNames.Hover)]
    [Description("Hover an element on the page")]
    public static async Task<string> Hover(
        [Description("CSS selector for element to hover")] string selector)
    {
        var page = await BrowserProxy.EnsurePage();
        await page.WaitForSelectorAsync(selector);
        await page.HoverAsync(selector);

        return "Hovered " + selector;
    }

    [McpTool(PlaywrightToolNames.Evaluate)]
    [Description("Execute JavaScript in the browser console")]
    public static async Task<string[]> Evaluate(
        [Description("JavaScript code to execute")] string script)
    {
        var page = await BrowserProxy.EnsurePage();
        var result = await page.EvaluateAsync(script);
        var resultJson = JsonSerializer.Serialize(result);

        return ["Evaluated script:", script, "with result:", resultJson];
    }

    [McpTool(PlaywrightToolNames.Close)]
    [Description("Close the browser and release all resources")]
    public static async Task<string> Close()
    {
        return await BrowserProxy.Close();
    }
}

public static class BrowserProxy
{
    private static IPage? _page;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    public static async Task<IPage> EnsurePage()
    {
        if (_page is null)
        {
            _playwright = await PW.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync();
            _page = await _browser.NewPageAsync();
        }

        return _page;
    }

    public static async Task<string> Close()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
            _page = null;
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
            _browser = null;
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
            _playwright = null;
        }

        return "Browser closed successfully";
    }
}

public static class PlaywrightToolNames
{
    public const string Navigate = "playwright_navigate";
    public const string Screenshot = "playwright_screenshot";
    public const string Click = "playwright_click";
    public const string IframeClick = "playwright_iframe_click";
    public const string Fill = "playwright_fill";
    public const string Select = "playwright_select";
    public const string Hover = "playwright_hover";
    public const string Evaluate = "playwright_evaluate";
    public const string Close = "playwright_close";
}
