# DayScope

DayScope is a lightweight Windows desktop dashboard for today's schedule.

It pulls events from Google Calendar, lays them out on a clean day timeline, highlights all-day events, shows attendance state, and can display a secondary time zone alongside the main schedule. The app is designed to stay out of the way: it starts as a small desktop companion and can be hidden to the system tray, where you can reopen it, trigger a manual refresh, or exit.

## Features

- Google Calendar integration
- Day timeline with overlapping event layout
- All-day event strip
- Event status styling for confirmed, tentative, declined, cancelled, and awaiting response
- Secondary time zone column
- Configurable schedule hours, window size, and refresh interval
- System tray integration with manual refresh

## Tech Stack

- .NET 10
- WPF
- Microsoft.Extensions.Hosting / DI / configuration
- Google Calendar API

## Project Structure

- `src/DayScope` - WPF desktop app
- `src/DayScope.Application` - application logic and dashboard composition
- `src/DayScope.Domain` - domain models and configuration objects
- `src/DayScope.Infrastructure` - Google Calendar integration, clock, and configuration wiring

## Requirements

- Windows
- .NET 10 SDK
- A Google Cloud OAuth client for desktop usage

## Configuration

The main settings file is:

- `src/DayScope/appsettings.json`

Important settings:

- `DaySchedule.StartHour` / `EndHour` - visible range of the day
- `DaySchedule.HourHeight` - vertical scale of the timeline
- `DaySchedule.SecondaryTimeZoneId` - optional second time zone
- `GoogleCalendar.Enabled` - turn calendar sync on or off
- `GoogleCalendar.CalendarId` - usually `primary`
- `GoogleCalendar.RefreshMinutes` - automatic refresh interval
- `GoogleCalendar.ClientSecretsPath` - path to your Google OAuth client JSON
- `GoogleCalendar.TokenStoreDirectory` - where OAuth tokens are cached

Default local paths use `%LocalAppData%\DayScope`.

## Google Calendar Setup

1. Create an OAuth client in Google Cloud for a desktop application.
2. Save the client JSON locally.
3. Put the file at the path from `GoogleCalendar.ClientSecretsPath`, or update that setting.
4. Start the app and complete the sign-in flow on first run.

## Run

```powershell
dotnet build .\src\DayScope.slnx
dotnet run --project .\src\DayScope\DayScope.csproj
```

## Tray Behavior

- The main window does not appear in the taskbar.
- Closing the window hides it to the system tray instead of exiting.
- Double-click the tray icon or use `Open` to show the window again.
- Use `Refresh now` in the tray menu to trigger a manual calendar refresh.
- Use `Exit` in the tray menu to fully close the application.

## Purpose

DayScope is intended to be a focused "what does my day look like right now?" view rather than a full calendar client. It favors quick readability, minimal friction, and an always-available desktop presence.
