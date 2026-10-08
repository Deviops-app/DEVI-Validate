# Devi.Theme

The shared WPF design system for the DEVI Windows apps. In this repository it is used by DEVI Validate. Merge `Themes/Devi.xaml` in `App.xaml`; it brings in tokens, typography, icons, and control styles. Colors and fonts come only from `Tokens.xaml` and `Typography.xaml`.

Keyed styles are opt-in. The implicit text field, password box, combo box, and date picker already use the shared focus ring, so those controls pick it up without a local style.

## Header icons: help, settings, more

Put the buttons in the content slot of `DeviPageHeader`, right-aligned, in this order: More (optional), Help, Settings.

```xml
<StackPanel Orientation="Horizontal" VerticalAlignment="Bottom">
  <Button Style="{StaticResource HeaderIconButton}" ToolTip="How to verify (F1)"
          AutomationProperties.Name="How to verify" Click="Help_Click">
    <Path Style="{StaticResource HeaderIcon}" Data="{StaticResource IconHelp}" />
  </Button>
  <Button Style="{StaticResource HeaderIconButton}" ToolTip="Settings (Ctrl+,)"
          AutomationProperties.Name="Settings" Click="Settings_Click">
    <Path Style="{StaticResource HeaderIcon}" Data="{StaticResource IconSettings}" />
  </Button>
</StackPanel>
```

- `HeaderIconButton`: 34 px circle, hairline border, hover and pressed fills, hand cursor, circular focus ring. Always set `ToolTip` and `AutomationProperties.Name`.
- `HeaderIcon` draws an icon path in the button's foreground. `HeaderIconDots` is the variant for `IconMore` (three dots).
- `DeviMenu` (ContextMenu) and `DeviMenuItem` (its `ItemContainerStyle`) style an overflow menu opened from the More button.
- Wire F1 to help and Ctrl+, to settings in the window's `OnPreviewKeyDown`.

## Settings shell

Use a `DeviDialog` window with a `DeviTitleBar ToolName="Settings"`, a scrolling body, and a footer with Cancel and Save. Sections go in this order, each a `Border` with `Settings.Section`, a `Settings.SectionTitle`, and a `Settings.SectionNote`:

1. Examiner profile: `<theme:ExaminerProfileEditor x:Name="ProfileEditor" />` (shared, same everywhere).
2. The app's own defaults (tool-specific).
3. Updates (say honestly how this app gets updates).
4. Data on this computer: the storage paths and a "Clear saved profile" button.

`DeviValidate.Desktop/SettingsWindow.xaml` is the reference implementation.

## Examiner profile and app settings (namespace `Devi.Theme.Profile`)

Local only. No network, no evidence data, never a case number.

| API | What it does |
| --- | --- |
| `ExaminerProfile.Load()` | Reads `%LOCALAPPDATA%\DEVI\profile.json`. A missing or unreadable file gives an empty profile. |
| `profile.Save()` | Trims fields, turns blanks into null, writes the file atomically. |
| `ExaminerProfile.Clear()` | Deletes the profile file. |
| `profile.IsEmpty` | True when no field is set (not serialized). |
| Fields | `ExaminerName`, `Agency`, `Unit`, `TitleOrBadge`, `Contact` (all optional strings). |
| `DeviLocalStore.AppSettingsPath("Decrypt")` | `%LOCALAPPDATA%\DEVI\Decrypt\settings.json` for one app's own settings class. |
| `DeviLocalStore.Load<T>(path)` / `Save<T>(path, value)` / `Delete(path)` | Generic camelCase JSON load and atomic save for any settings class with a parameterless constructor. |
| `ExaminerProfileEditor.Show(profile)` / `Read()` / `FocusFirst()` | Fills the five shared fields, reads them back, focuses the first one. The host dialog decides when to save. |

Autofill rule used by Validate: on launch and on Start new, fill Examiner and Agency from the profile. After Settings is saved, fill only fields that are still empty.

## Other shared styles

- `FieldRing`: the border on every text field. Idle is a 1px hairline. Focus is one 2px blue-to-purple gradient, the same stops as deviops.app, painted as two filled rounded rectangles so the corner is the same thickness as the sides. There is no second focus rectangle. `BareTextBox` and `BarePasswordBox` stay borderless for a control that already sits inside a ring, such as the passphrase reveal and the expected-hash box.
- `SectionExpander`: collapsible section with a full-width clickable header, a Show/Hide pill with a chevron, hover fill, hand cursor, and a focus ring.
- `EditableComboBox`: a pick-or-type combo box. Put the hint in `Tag`.
- `RequiredMark` (a small "Required" label) and `FieldError` (a red message under a field, collapsed until shown).

## App icon

The shared Windows app icon lives in `shared/brand` (`devi-app.ico` and its generator). See the README there.
