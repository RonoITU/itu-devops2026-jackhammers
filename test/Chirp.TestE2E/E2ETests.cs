namespace Chirp.TestE2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class E2ETests : PageTest
{
    private const string AppUrl = "http://localhost:5273";
    private string _startupProjectPath;
    private Process? _appProcess;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    // Provide a non-nullable accessor without hiding PageTest.Page
    private IPage CurrentPage => _page ?? throw new InvalidOperationException("Page has not been initialized. Ensure SetUp has run and page creation succeeded.");
    private const string TestUsername = "Tester";
    private const string TestUserEmail = "testuser@gmail.com";
    private const string TestUserPassword = "Test@12345";

    readonly BrowserTypeLaunchOptions _browserTypeLaunchOptions = new BrowserTypeLaunchOptions
    {
        Headless = true
    };

    [SetUp]
    public async Task Setup()
    {
        // Check if app process is still running
        if (_appProcess == null || _appProcess.HasExited)
        {
            throw new Exception($"Application process has exited. Exit code: {_appProcess?.ExitCode}");
        }

        Console.WriteLine(_startupProjectPath);
        _browser = await Playwright.Chromium.LaunchAsync(_browserTypeLaunchOptions);
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();

        if (_page == null) throw new InvalidOperationException("Page is not initialized");
    }
    /// <summary>
    /// One time Setup for setting up the process of running the test environment
    /// </summary>
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.."));
        _startupProjectPath = Path.Combine(solutionDirectory, "src", "Chirp.Web", "Chirp.Web.csproj");

        _appProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_startupProjectPath}\" test",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        _appProcess.Start();
        _appProcess.BeginOutputReadLine();
        _appProcess.BeginErrorReadLine();

        // Wait for app to be ready with retries
        var maxRetries = 30;
        var retryDelay = TimeSpan.FromSeconds(1);
        var isReady = false;

        using var httpClient = new HttpClient();
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await httpClient.GetAsync($"{AppUrl}");
                if (response.IsSuccessStatusCode)
                {
                    isReady = true;
                    Console.WriteLine($"App is ready after {i + 1} attempts");
                    break;
                }
            }
            catch
            {
                // App not ready yet
            }
        
            await Task.Delay(retryDelay);
        }

        if (!isReady)
        {
            throw new Exception("Application failed to start within the expected time");
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Stop the ASP.NET application
        if (_appProcess is { HasExited: false })
        {
            _appProcess.Kill();
            _appProcess.WaitForExit(5000); //closes after specified time
            _appProcess.Dispose();
        }
        
        // Dispose of the browser context
        _context?.DisposeAsync().GetAwaiter().GetResult();

        // Dispose of the browser
        _browser?.DisposeAsync().GetAwaiter().GetResult();
        
        // Delete the test database file
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.."));
        var testDbFilePath = Path.Combine(solutionDirectory, "src", "Chirp.Infrastructure", "Data", "CheepTest.db");
        string walFilePath = testDbFilePath + "-wal";
        string shmFilePath = testDbFilePath + "-shm";
        
        // Check if the database file exists and delete it
        if (File.Exists(testDbFilePath))
        {
            File.Delete(testDbFilePath);
        }
        // Check if the WAL file exists and delete it
        if (File.Exists(walFilePath))
        {
            File.Delete(walFilePath);
        }
        // Check if the SHM file exists and delete it
        if (File.Exists(shmFilePath))
        {
            File.Delete(shmFilePath);
        }
    }
    
    //---------------------------------- HELPER METHODS ----------------------------------
    // Register
    private async Task RegisterUser(String? userCount = "")
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Register");

        // Arrived at register page, and put in email and password
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync(TestUsername + userCount);
        await CurrentPage.GetByPlaceholder("name@example.com").ClickAsync();
        await CurrentPage.GetByPlaceholder("name@example.com").FillAsync(TestUserEmail + userCount);
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync(TestUserPassword + userCount);
        await CurrentPage.GetByPlaceholder("Confirm password").ClickAsync();
        await CurrentPage.GetByPlaceholder("Confirm password").FillAsync(TestUserPassword + userCount);

        // Clicks on the register button to register the account
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
    }

    // Login
    private async Task LoginUser(String? userCount = "")
    {
        // Goes to login page
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Login");

        // Fills in information
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync(TestUsername + userCount);
        await CurrentPage.GetByPlaceholder("password").ClickAsync();
        await CurrentPage.GetByPlaceholder("password").FillAsync(TestUserPassword + userCount);

        // Clicks on log in button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
    }

    // Logout
    private async Task LogoutUser()
    {
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Logout" }).ClickAsync();
    }

    // Delete 
    private async Task DeleteUser()
    {
        // Removing the test user
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Manage");
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Personal data" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Forget me!" }).ClickAsync();
    }

    //---------------------------------- PUBLIC TIMELINE TESTS ----------------------------------
    [Test]
    [Category("End2End")]
    [Category("PublicTimeline")]
    public async Task LoadPublicTimeline()
    {
        await CurrentPage.GotoAsync($"{AppUrl}");
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Public Timeline" })).ToBeVisibleAsync();
    }

    [Test]
    [Category("End2End")]
    [Category("PublicTimeline")]
    public async Task PublicTimelineLoadingCheeps()
    {
        await CurrentPage.GotoAsync($"{AppUrl}");
        await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
    }

    [Test]
    [Category("End2End")]
    [Category("PublicTimeline")]
    public async Task PublicTimelineNextAndPreviousPage()
    {
        await CurrentPage.GotoAsync($"{AppUrl}");

        // If there is a next page button
        if (await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">", Exact = true }).CountAsync() > 0)
        {
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "<", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
        }
    }

    [Test]
    [Category("End2End")]
    [Category("PublicTimeline")]
    public async Task PublicTimelineFirstAndLastPage()
    {
        await CurrentPage.GotoAsync($"{AppUrl}");

        // If there is a next page button
        if (await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">", Exact = true }).CountAsync() > 0)
        {
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">>", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "<<", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
        }
    }

    //---------------------------------- USER TIMELINE TESTS ----------------------------------
    [Test]
    [Category("End2End")]
    public async Task DoesUserTimelinePageSuccessfullyLoad()
    {
        // Go to Adrian's page
        await CurrentPage.GotoAsync($"{AppUrl}/Adrian");
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Adrian's Timeline" })).ToBeVisibleAsync();
    }

    // Verify that clicking on a user goes to their timeline
    [Test]
    [Category("End2End")]
    public async Task GoToUserTimelineFromUsername()
    {
        await CurrentPage.GotoAsync($"{AppUrl}");

        var firstMessageLink = CurrentPage.Locator("#messagelist > li:first-child a").Nth(0);

        var name = await firstMessageLink.InnerTextAsync();

        await firstMessageLink.ClickAsync();

        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = $"{name}'s Timeline" })).ToBeVisibleAsync();
    }

    // Check for presence of cheeps for some author
    [Test]
    [Category("End2End")]
    public async Task PresenceOfCheeps()
    {
        // Go to Adrian's page
        await CurrentPage.GotoAsync($"{AppUrl}/Adrian");
        await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
    }

    // Check for no cheeps on user timeline with no cheeps
    [Test]
    [Category("End2End")]
    public async Task NoCheepsOnUserTimeline()
    {
        await RegisterUser();
        await LoginUser();
        
        // Go to a user page with no cheeps
        await CurrentPage.GotoAsync($"{AppUrl}/{TestUsername}");
        await Expect(CurrentPage.GetByText("There are no cheeps so far.")).ToBeVisibleAsync();
        
        await DeleteUser();
    }
    
    // Check for no cheeps on user timeline with no cheeps
    [Test]
    [Category("End2End")]
    public async Task NoUserRedirectToPublicTimeline()
    {
        // Go to a user page with no cheeps
        await CurrentPage.GotoAsync($"{AppUrl}/AUserThatDoesNotExist");
        await Expect(CurrentPage.GetByText("Public Timeline")).ToBeVisibleAsync();
    }

    // Check back button goes to public timeline
    [Test]
    [Category("End2End")]
    public async Task BackButtonGoesToPublicTimeline()
    {
        // Go to Adrian's page
        await CurrentPage.GotoAsync($"{AppUrl}/Adrian");

        // Click on the back button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();

        // Check if the public timeline is visible
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Public Timeline" })).ToBeVisibleAsync();
    }

    // Check next and previous buttons on user timeline
    [Test]
    [Category("End2End")]
    [Category("UserTimeline")]
    public async Task UserTimelineNextAndPreviousPage()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Darth%20Vader");

        // If there is a next page button
        if (await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">", Exact = true }).CountAsync() > 0)
        {
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "<", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
        }
    }

    // Check first and last page buttons on user timeline
    [Test]
    [Category("End2End")]
    [Category("UserTimeline")]
    public async Task UserTimelineFirstAndLastPage()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Darth%20Vader");
        
        if (await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">", Exact = true }).CountAsync() > 0)
        {
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = ">>", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "<<", Exact = true }).ClickAsync();
            await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
        }
    }

    //---------------------------------- PERSONAL TIMELINE TESTS ----------------------------------
    [Test]
    [Category("End2End")]
    public async Task GoToPersonalTimeline()
    {
        await RegisterUser();

        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "My timeline" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = $"{TestUsername}'s Timeline" }))
             .ToBeVisibleAsync();

        await DeleteUser();
    }


    //---------------------------------- REGISTER PAGE TESTS ----------------------------------

    // Registration page loads successfully (Expect the registration form)
    [Test]
    [Category("End2End")]
    public async Task RegisterPageLoads()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Register");

        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Create a new account." })).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByPlaceholder("Username")).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByPlaceholder("Name@example.com")).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByPlaceholder("Password", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByPlaceholder("Confirm password")).ToBeVisibleAsync();
    }

    // Successfully registration with valid inputs
    [Test]
    [Category("End2End")]
    public async Task SuccessfulRegister()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Register");

        // Arrived at register page, and put in email and password
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync(TestUsername);
        await CurrentPage.GetByPlaceholder("name@example.com").ClickAsync();
        await CurrentPage.GetByPlaceholder("name@example.com").FillAsync(TestUserEmail);
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync(TestUserPassword);
        await CurrentPage.GetByPlaceholder("Confirm Password").ClickAsync();
        await CurrentPage.GetByPlaceholder("Confirm Password").FillAsync(TestUserPassword);

        // Clicks on the register button to register the account
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();

        // Person has correctly registered if logout button is visible
        await Expect(CurrentPage.GetByRole(AriaRole.Link, new() { Name = $"Logout" })).ToBeVisibleAsync();
        
        // Clean up
        await LoginUser();
        await DeleteUser();
    }

    // Registration without @ in email
    [Test]
    [Category("End2End")]
    public async Task RegisterWithoutAtInEmail()
    {
        // Listen for the dialog event
        Page.Dialog += async (_, dialog) =>
        {
            // Verify the text of the popup
            if (dialog.Message.Contains("Mailadressen skal indeholde et \"@\". \"emailwithoutat\" mangler et \"@\"."))
            {
                // Accept the popup
                await Expect(CurrentPage.GetByText("Mailadressen skal indeholde et \"@\". \"emailwithoutat\" mangler et \"@\".")).ToBeVisibleAsync();
            }
        };

        // Attempt to register with an invalid email
        await CurrentPage.GotoAsync("http://localhost:5273/Identity/Account/Register");
        await CurrentPage.GetByPlaceholder("name@example.com").FillAsync("emailwithoutat");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("MyBadAccount");
        await CurrentPage.GetByPlaceholder("Confirm Password").FillAsync("MyBadAccount");
    }

    // Registration with password not living up to constraint (at least one nonalphanumeric character)
    [Test]
    [Category("End2End")]
    public async Task RegisterWithNoAlphanumericCharacter()
    {
        await CurrentPage.GotoAsync("http://localhost:5273/Identity/Account/Register");
        await CurrentPage.GetByPlaceholder("Username").FillAsync("myusername");
        await CurrentPage.GetByPlaceholder("name@example.com").FillAsync("my@mail.com");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("BadPassword1234");
        await CurrentPage.GetByPlaceholder("Confirm Password").FillAsync("BadPassword1234");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        await Expect(CurrentPage.GetByText("Passwords must have at least one non alphanumeric character."))
            .ToBeVisibleAsync();
    }

    // Registration with password not living up to constraints (at least one digit ('0'-'9'))
    [Test]
    [Category("End2End")]
    public async Task RegisterWithNoDigit()
    {
        await CurrentPage.GotoAsync("http://localhost:5273/Identity/Account/Register");
        await CurrentPage.GetByPlaceholder("Username").FillAsync("myusername");
        await CurrentPage.GetByPlaceholder("name@example.com").FillAsync("my@mail.com");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("BadPassword!");
        await CurrentPage.GetByPlaceholder("Confirm Password").FillAsync("BadPassword!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        await Expect(CurrentPage.GetByText("Passwords must have at least one digit ('0'-'9').")).ToBeVisibleAsync();
    }

    // Registration with password not living up to constraints (at least one uppercase ('A'-'Z'))
    [Test]
    [Category("End2End")]
    public async Task RegisterWithNoUppercase()
    {
        await CurrentPage.GotoAsync("http://localhost:5273/Identity/Account/Register");
        await CurrentPage.GetByPlaceholder("Username").FillAsync("myusername");
        await CurrentPage.GetByPlaceholder("name@example.com").FillAsync("my@mail.com");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("badpassword1234!");
        await CurrentPage.GetByPlaceholder("Confirm Password").FillAsync("badpassword1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        await Expect(CurrentPage.GetByText("Passwords must have at least one uppercase ('A'-'Z').")).ToBeVisibleAsync();
    }

    //---------------------------------- LOGIN PAGE TESTS ----------------------------------

    // Login page loads successfully (check for login form)
    [Test]
    [Category("End2End")]
    public async Task LoginPageLoads()
    {
        await CurrentPage.GotoAsync("http://localhost:5273/Identity/Account/Login");

        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Use a local account to log in." }))
            .ToBeVisibleAsync();
        await Expect(CurrentPage.GetByPlaceholder("Username")).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByPlaceholder("password")).ToBeVisibleAsync();
    }

    // Test successfully login
    [Test]
    [Category("End2End")]
    public async Task LoginSuccessfully()
    {
        await RegisterUser();
        await LogoutUser();

        // Goes to login page
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Login");

        // Fills in information
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync(TestUsername);
        await CurrentPage.GetByPlaceholder("password").ClickAsync();
        await CurrentPage.GetByPlaceholder("password").FillAsync(TestUserPassword);

        // Clicks on log in button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        // User arrived at the homepage and should now see a logout button with their email attached
        await Expect(CurrentPage.GetByRole(AriaRole.Link, new() { Name = $"Logout" })).ToBeVisibleAsync();

        await DeleteUser();
    }

    // Login with invalid credentials
    [Test]
    [Category("End2End")]
    public async Task NoRegisterInvalidLogin()
    {
        // Goes to login page
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Login");

        // Fills in information
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync(TestUsername);
        await CurrentPage.GetByPlaceholder("password").ClickAsync();
        await CurrentPage.GetByPlaceholder("password").FillAsync(TestUserPassword);

        // Clicks on log in button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(CurrentPage.GetByText("Invalid login attempt.")).ToBeVisibleAsync();
    }

    // Login with no email entered
    [Test]
    [Category("End2End")]
    public async Task LoginWithNoEmailEntered()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Login");
        // Fills in information
        await CurrentPage.GetByPlaceholder("password").ClickAsync();
        await CurrentPage.GetByPlaceholder("password").FillAsync(TestUserPassword);

        // Clicks on log in button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(CurrentPage.GetByText("The Username field is required.")).ToBeVisibleAsync();
    }

    // Login with no password entered
    [Test]
    [Category("End2End")]
    public async Task LoginWithNoPasswordEntered()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Login");
        // Fills in information
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync(TestUsername);

        // Clicks on log in button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(CurrentPage.GetByText("The Password field is required.")).ToBeVisibleAsync();
    }


    // Check 'register as a new user' redirects to registration page.
    [Test]
    [Category("End2End")]
    public async Task LoginPageLinkRedirectToRegistrationPage()
    {
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Login");
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Register as a new user" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Create a new account." })).ToBeVisibleAsync();
    }
    
   
    //---------------------------------- LOGOUT PAGE TESTS ----------------------------------

    // The logout button logs user out (check for no authentication and redirect)
    [Test]
    [Category("End2End")]
    public async Task LogoutButtonWorks()
    {
        await RegisterUser();
        
        await CurrentPage.GotoAsync($"{AppUrl}");
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Logout" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Log in", Exact = true })).ToBeVisibleAsync();
        
        // Clean up
        await LoginUser();
        await DeleteUser();
    }
    
    //---------------------------------- MANAGE ACCOUNT TESTS ----------------------------------
    
    // Manage page loads successfully
    [Test]
    [Category("End2End")]
    public async Task LoadManageAccountPage()
    {
        await RegisterUser();
        
        await CurrentPage.GotoAsync($"{AppUrl}");
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Manage account" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Manage your account" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    // Personal data page loads successfully
    [Test]
    [Category("End2End")]
    public async Task LoadManageUserPage()
    {
        await RegisterUser();
        
        await CurrentPage.GotoAsync($"{AppUrl}/Identity/Account/Manage");
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Personal data" }).ClickAsync();
        
        await Expect(CurrentPage.GetByRole(AriaRole.Heading, new() { Name = "Personal Data" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    // Check that users don't follow authors that are deleting.
    [Test]
    [Category("End2End")]
    public async Task StopFollowingDeletedUser()
    {
        await CurrentPage.GotoAsync($"{AppUrl}");
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Register Symbol Register" }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync("bob1");
        await CurrentPage.GetByPlaceholder("Name@example.com").ClickAsync();
        await CurrentPage.GetByPlaceholder("Name@example.com").FillAsync("bob1@bob.dk");
        await CurrentPage.GetByPlaceholder("Name@example.com").PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("Bob1234!");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Confirm password").FillAsync("Bob1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("hej");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Logout" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Register Symbol Register" }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync("bob2");
        await CurrentPage.GetByPlaceholder("Username").PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Name@example.com").FillAsync("bob2@bob.dk");
        await CurrentPage.GetByPlaceholder("Name@example.com").PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("Bob1234!");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Confirm password").FillAsync("Bob1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        await CurrentPage.Locator("li").Filter(new() { HasText = "bob1 Follow" }).Locator("#followButton").First.ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Logout" }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync("bob1");
        await CurrentPage.GetByPlaceholder("Password").ClickAsync();
        await CurrentPage.GetByPlaceholder("Password").FillAsync("Bob1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Manage account" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Personal data" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Forget me!" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Register Symbol Register" }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync("bob1");
        await CurrentPage.GetByPlaceholder("Name@example.com").ClickAsync();
        await CurrentPage.GetByPlaceholder("Name@example.com").FillAsync("bob1@bob.dk");
        await CurrentPage.GetByPlaceholder("Name@example.com").PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Password", new() { Exact = true }).FillAsync("Bob1234!");
        await CurrentPage.GetByPlaceholder("Confirm password").ClickAsync();
        await CurrentPage.GetByPlaceholder("Confirm password").FillAsync("Bob1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("hejsa");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Logout" }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync("bob2");
        await CurrentPage.GetByPlaceholder("Password").ClickAsync();
        await CurrentPage.GetByPlaceholder("Password").FillAsync("Bob1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        
        // Assert
        await Expect(CurrentPage.GetByText("bob1 Follow")).ToBeVisibleAsync();
        
        // Clean up
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Manage account" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Personal data" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Forget me!" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Login" }).ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").ClickAsync();
        await CurrentPage.GetByPlaceholder("Username").FillAsync("bob1");
        await CurrentPage.GetByPlaceholder("Username").PressAsync("Tab");
        await CurrentPage.GetByPlaceholder("Password").FillAsync("Bob1234!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Manage account" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Personal data" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Forget me!" }).ClickAsync();
    }
    
    // Personal data page loads successfully
    [Test]
    [Category("End2End")]
    public async Task DeleteUserCheeps()
    {
        await RegisterUser();
        
        await CurrentPage.GotoAsync($"{AppUrl}");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("My message");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("So nice");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        await Expect(CurrentPage.GetByText("There are no cheeps so far.")).Not.ToBeVisibleAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Login Symbol Manage account" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Personal data" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Delete my Cheeps" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        await Expect(CurrentPage.GetByText("There are no cheeps so far.")).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByText("There are no cheeps so far.")).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    
    
    //---------------------------------- CHEEPS TESTS ----------------------------------
    
    // Testing Successful cheep line after login
    [Test]
    [Category("End2End")]
    public async Task TestShareCheepsVisibilityPublicTimeline()
    {
        await RegisterUser();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "public timeline" }).ClickAsync();
        await Expect(CurrentPage.GetByText($"What's on your mind {TestUsername}? Share")).ToBeVisibleAsync();
        
        
        // Clean up
        await DeleteUser();   
    }
    [Test]
    [Category("End2End")]
    public async Task TestShareCheepsVisibilityPrivateTimeline()
    {
        await RegisterUser();
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "My timeline" }).ClickAsync();
        await Expect(CurrentPage.GetByText($"What's on your mind {TestUsername}? Share")).ToBeVisibleAsync();
        
        
        // Clean up
        await DeleteUser();   
    }
    [Test]
    [Category("End2End")]
    public async Task CheepingCheeps()
    {
        await RegisterUser();
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "public timeline" }).ClickAsync();
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("Hello World!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await Expect(CurrentPage.Locator("li").Filter(new() { HasText = "Hello World!" }).First).ToBeVisibleAsync();

        
        // Clean up and delete data
        await DeleteUser();   
    }

    [Test]
    [Category("End2End")]
    public async Task CheepingFromPrivateTimeline()
    {
        {
            await RegisterUser();
    
            await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
            await CurrentPage.Locator("#CheepText").ClickAsync();
            await CurrentPage.Locator("#CheepText").FillAsync("Hello");
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
            await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
            await Expect(CurrentPage.GetByText("Hello")).ToBeVisibleAsync();
    
        
            // Clean up and delete data
            await DeleteUser();   
        }
    }
    [Test]
    [Category("End2End")]
    public async Task EmptyCheeps()
    {
        await RegisterUser();
        
        await CurrentPage.GotoAsync($"{AppUrl}");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await Expect(CurrentPage.GetByText("At least write something")).ToBeVisibleAsync();

        // Clean up and delete data
        await DeleteUser();   
    }
    [Test]
    [Category("End2End")]
    public async Task LongCheeps()
    {
        {
            await RegisterUser();
            
            await CurrentPage.GotoAsync($"{AppUrl}");
            await CurrentPage.Locator("#CheepText").ClickAsync();
            await CurrentPage.Locator("#CheepText").FillAsync("Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message Very Long Message ");
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
            await Expect(CurrentPage.GetByText("Maximum length is 160")).ToBeVisibleAsync();
        
            // Clean up and delete data
            await DeleteUser();   
        }
    }
    [Test]
    [Category("End2End")]
    public async Task DeletedCheeps()
    {
        {
            await RegisterUser();
            
            
            await CurrentPage.GotoAsync($"{AppUrl}");
            await CurrentPage.Locator("#CheepText").ClickAsync();
            await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
            await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
            // Clean up and delete data
            await DeleteUser();   
            
            // check that the cheep is deleted
            await Expect(CurrentPage.Locator("li").Filter(new() { HasText = "HelloWorld!RasmusMathiasNikolajMarcusErTelos!" }).First).Not.ToBeVisibleAsync();
        }
    }

    //---------------------------------- FOLLOWING TESTS ----------------------------------
    
    [Test]
    [Category("End2End")]
    public async Task DoesFollowButtonLoad()
    {
        await RegisterUser();

        await Expect(CurrentPage.Locator("li").Locator("#followButton").First).ToBeVisibleAsync();
  
        
        // Clean up
        await DeleteUser();
    }
    
    [Test]
    [Category("End2End")]
    public async Task DoesUnfollowButtonLoad()
    {
        await RegisterUser();

        await CurrentPage.Locator("li").Locator("#followButton").First.ClickAsync();
        
        await Expect(CurrentPage.Locator("li").Locator("#unfollowButton").First).ToBeVisibleAsync();
        
        await CurrentPage.Locator("li").Locator("#unfollowButton").First.ClickAsync();
        
        // Clean up
        await DeleteUser();
    }


    [Test]
    [Category("End2End")]
    public async Task DoesFollowedAuthorLoadCheeps()
    {
        await RegisterUser();

        await CurrentPage.Locator("li").Locator("#followButton").First.ClickAsync();
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        
        await Expect(CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Unfollow" })).ToBeVisibleAsync();
        
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Unfollow" }).ClickAsync();
        
        // Clean up
        await DeleteUser();
    }

    /*---------------------------------- FOLLOWING LISTS TESTS ----------------------------------*/
    [Test]
    [Category("End2End")]
    public async Task DoesFollowListDisplayCorrectAmountFollowers()
    {
        // Follows another user and checks if the count for following is 1 and then unfollows the user and checks if the count is 0
        await RegisterUser("1");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        
        await RegisterUser("2");

        await CurrentPage.Locator("li").First.Locator("#followButton").ClickAsync();
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        
        await Expect(CurrentPage.Locator("body")).ToContainTextAsync("Following: 1");
        
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Unfollow" }).ClickAsync();
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        
        await Expect(CurrentPage.Locator("body")).ToContainTextAsync("Followers: 0");
        
        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();    
    }
    
    [Test]
    [Category("End2End")]
    public async Task DoesFollowListDisplayCorrectAmountFollowing()
    {
        // Follows another user and checks if the count for followers is 1 and then unfollows the user and checks if the count is 0

        await RegisterUser("1");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync($"HelloWorld!RasmusMathiasNikolajMarcusErTelos!+{DateTime.Now:HH:mm:ss}");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        
        await RegisterUser("2");
        
        await CurrentPage.Locator("li").First.Locator("#followButton").ClickAsync();

        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Tester1" }).ClickAsync();
        
        await Expect(CurrentPage.Locator("body")).ToContainTextAsync("Followers: 1");
        
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Unfollow" }).ClickAsync();
        
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Tester1" }).ClickAsync();
        
        await Expect(CurrentPage.Locator("body")).ToContainTextAsync("Followers: 0");
        
        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();
    }
    [Test]
    [Category("End2End")]
    public async Task DoesFollowPopupDisplayCorrectFollowers()
    {
        // Follows another user and checks if a user shows up in the follower list popup
        await RegisterUser("1");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        
        await RegisterUser("2");
    
        await CurrentPage.Locator("li").First.Locator("#followButton").ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Following:" }).ClickAsync();
        await Expect(CurrentPage.Locator("#popup2").GetByRole(AriaRole.Button, new() { Name = "Unfollow" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();
    }
    
    [Test]
    [Category("End2End")]
    public async Task DoesFollowPopupDisplayCorrectFollowing()
    {
        // Follows another user and checks if a user shows up in the following list popup

        await RegisterUser();
    
        await CurrentPage.Locator("li").First.Locator("#followButton").ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol My timeline" }).ClickAsync();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Following:" }).ClickAsync();
        await Expect(CurrentPage.Locator("#popup2").GetByRole(AriaRole.Button, new() { Name = "Unfollow" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    // ---------------------------------- Delete TESTS ----------------------------------
    [Test]
    [Category("End2End")]
    // Test that the cheep is being deleted
    public async Task DoesDeleteButtonLoad()
    {
        await RegisterUser();
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("Testing that this is a deleteable cheep");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await Expect(CurrentPage.Locator("li").Filter(new() { HasText = "Testing that this is a deleteable cheep" }).First).ToBeVisibleAsync();
        await CurrentPage.Locator("#deleteButton").ClickAsync();
        await Expect(CurrentPage.Locator("li").Filter(new() { HasText = "Testing that this is a deleteable cheep" }).First).Not.ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    /*---------------------------------- REACTIONS TESTS ----------------------------------*/
    [Test]
    [Category("End2End")]
    public async Task LikeReactionTest()
    {
        // Like reaction another users post and check if the corresponding reaction shows up
        await RegisterUser("1");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        await RegisterUser("2");
        
        // Hover over the like button to reveal the reactions
        await CurrentPage.Locator("li").First.Locator("#likeMethod").HoverAsync();

        // Click the reaction button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "👍" }).First.ClickAsync();
        
        await Expect(CurrentPage.Locator("span").Filter(new() { HasText = "👍" }).First).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();    
    }
    
    [Test]
    [Category("End2End")]
    public async Task DislikeReactionTest()
    {
        // Dislike reaction another users post and check if the corresponding reaction shows up
        await RegisterUser("1");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        await RegisterUser("2");
        
        // Hover over the dislike button to reveal the reactions
        await CurrentPage.Locator("li").First.Locator("#dislikeMethod").HoverAsync();

        // Click the reaction button
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "👎" }).First.ClickAsync();
        
        await Expect(CurrentPage.Locator("span").Filter(new() { HasText = "👎" })).ToBeVisibleAsync();

        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();    
    }
    
    [Test]
    [Category("End2End")]
    public async Task SwitchFromLikeToDislikeReactionTest()
    {
        // Dislike reaction another users post and switch to Like reaction and check if the corresponding reaction shows up
        await RegisterUser("1");
        
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        await RegisterUser("2");
        
        await CurrentPage.Locator("li").First.Locator("#likeMethod").HoverAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "👍" }).First.ClickAsync();
        await CurrentPage.Locator("li").First.Locator("#dislikeMethod").HoverAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "👎" }).First.ClickAsync();
        
        await Expect(CurrentPage.Locator("span").Filter(new() { HasText = "👎" })).ToBeVisibleAsync();


        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();    
    }

    [Test]
    [Category("End2End")]
    public async Task SwitchFromDislikeToLikeReactionTest()
    {
        // Like reaction another users post and switch to Dislike reaction and check if the corresponding reaction shows up
        await RegisterUser("1");

        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("HelloWorld!RasmusMathiasNikolajMarcusErTelos!");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await LogoutUser();
        await RegisterUser("2");

        await CurrentPage.Locator("li").First.Locator("#dislikeMethod").HoverAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "👎" }).First.ClickAsync();
        await CurrentPage.Locator("li").First.Locator("#likeMethod").HoverAsync();
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "👍" }).First.ClickAsync();

        await Expect(CurrentPage.Locator("span").Filter(new() { HasText = "👍" }).First).ToBeVisibleAsync();


        // Clean up
        await DeleteUser();
        await LoginUser("1");
        await DeleteUser();
    }

//---------------------------------- Image TESTS  ----------------------------------
    [Test]
    [Category("SkipOnGitHubActions")]
    public async Task CanUserUploadImage()
    {
        await RegisterUser();
        
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.."));
        var imagePath = Path.Combine(solutionDirectory, "src", "Chirp.Web", "wwwroot", "images", "icon1.png");
        
        await CurrentPage.Locator("#CheepImage").ClickAsync();
        await CurrentPage.Locator("#CheepImage").SetInputFilesAsync([imagePath]);
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("Hej");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Img, new() { Name = "Cheep Image" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    [Test]
    [Category("SkipOnGitHubActions")]
    public async Task CanUserUploadGif()
    {
        await RegisterUser();
        
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.."));
        var imagePath = Path.Combine(solutionDirectory, "src", "Chirp.Web", "wwwroot", "images", "TESTGIF.gif");
        
        await CurrentPage.Locator("#CheepImage").ClickAsync();
        await CurrentPage.Locator("#CheepImage").SetInputFilesAsync([imagePath]);
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("Hej");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Img, new() { Name = "Cheep Image" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    
    // ---------------------------------- Comment TESTS ----------------------------------
    [Test]
    [Category("End2End")]
    public async Task DoesCommentDeleteButtonLoad()
    {
        await RegisterUser();
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("CreateCheepTest");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await CurrentPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(CurrentPage.GetByRole(AriaRole.Button, new() { Name = "View Comments" }).First).ToBeVisibleAsync();
        await  CurrentPage.GetByRole(AriaRole.Button, new() { Name = "View Comments" }).First.ClickAsync();
        await CurrentPage.GetByPlaceholder("Answer Tester").ClickAsync();
        await CurrentPage.GetByPlaceholder("Answer Tester").FillAsync("CreateCommentTest");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Add Comment" }).ClickAsync();
        await Expect(CurrentPage.GetByRole(AriaRole.Button, new() { Name = "\uf1f8" })).ToBeVisibleAsync();
        
        // Clean up
        await DeleteUser();
    }
    [Test]
    [Category("End2End")]
    public async Task CommentPublicAvalable()
    {
        await RegisterUser();
        await CurrentPage.Locator("#CheepText").ClickAsync();
        await CurrentPage.Locator("#CheepText").FillAsync("TestCreateCheep");
        await CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await LogoutUser();
        await CurrentPage.GetByRole(AriaRole.Link, new() { Name = "Home Symbol Timeline" }).ClickAsync();
        
        await Expect(CurrentPage.GetByRole(AriaRole.Button, new() { Name = "View Comments" }).First).ToBeVisibleAsync();
        await  CurrentPage.GetByRole(AriaRole.Button, new() { Name = "View Comments" }).First.ClickAsync();
        
        await LoginUser();
        
        // Clean up
        await DeleteUser();
    }
    [Test]
    [Category("End2End")]
    public async Task CommentTestLocator()
    {
        await RegisterUser();

        await Expect(CurrentPage.GetByRole(AriaRole.Button, new() { Name = "View Comments" }).First).ToBeVisibleAsync();
        await  CurrentPage.GetByRole(AriaRole.Button, new() { Name = "View Comments" }).First.ClickAsync();
        await Expect(CurrentPage.GetByText("Back Comment section")).ToBeVisibleAsync();

        // Clean up
        await DeleteUser();
    }
    
    
    
}
