# GitHub Readme Stats

A .NET 9 Web API that generates customizable SVG cards displaying GitHub user and repository statistics for embedding in README files.

## Features

- **Streak Stats Card** - Current streak, longest streak, and total contributions with animated SVG
- **User Stats Card** - Stars, commits, PRs, reviews, followers, and rank
- **Top Languages Card** - Most used programming languages
- **Repository Card** - Pinned repository information
- **Gist Card** - Gist statistics


## Prerequisites

- .NET 9.0 SDK
- Redis (optional, for distributed caching)

## Docker Instructions

### 1. Build the Docker Image

Open a terminal in the project root and run:

```powershell
docker build -t github-readme-stats .
```

### 2. Run the Container

```powershell
docker run -v ${PWD}/appsettings.json:/app/appsettings.json -p 8080:8080 github-readme-stats
```

- Replace `8080` with your desired host port if needed.

### Alternative: Using Docker Compose

If you want to use Docker Compose (recommended for development or multi-service setups):

```powershell
docker-compose up --build
```

- This will build and start all services defined in `docker-compose.yml`.


## Getting Started (Compile)

```bash
# Build
cd src
dotnet build GitHubStats.sln

# Run
cd GitHubStats.Api
dotnet run
```

The API will be available at `http://localhost:5042`.

## API Endpoints

| Endpoint                               | Description            |
|----------------------------------------|------------------------|
| `/api/streak?username={user}`          | Streak statistics card |
| `/api/stats?username={user}`           | User statistics card   |
| `/api/top-langs?username={user}`       | Top languages card     |
| `/api/pin?username={user}&repo={repo}` | Repository pin card    |
| `/api/gist?id={gist_id}`               | Gist card              |
| `/health`                              | Health check           |

## Usage

Add to your GitHub README:

```markdown
![GitHub Streak](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid)
```

### Streak Card Options

| Parameter                  | Description                               | Default   |
|----------------------------|-------------------------------------------|-----------|
| `username`                 | GitHub username (required)                | -         |
| `theme`                    | Card theme                                | `default` |
| `hide_border`              | Hide card border                          | `false`   |
| `border_radius`            | Card corner radius                        | `4.5`     |
| `card_width`               | Manual card width override                | `495`     |
| `card_height`              | Manual card height override               | `150`     |
| `hide_total_contributions` | Hide total contributions section          | `false`   |
| `hide_current_streak`      | Hide current streak section               | `false`   |
| `hide_longest_streak`      | Hide longest streak section               | `false`   |
| `starting_year`            | Start calculating streak from this year   | `2005`    |
| `fire_color`               | Color of the fire icon (HEX)              | `ff6b6b`  |
| `curr_streak_num_color`    | Color of the current streak number        | -         |
| `side_nums_color`          | Color of the side numbers (total/longest) | -         |
| `curr_streak_label_color`  | Color of the "Current Streak" label       | -         |
| `side_labels_color`        | Color of the side labels                  | -         |
| `dates_color`              | Color of the date range text              | -         |
| `cache_seconds`            | Custom cache TTL in seconds               | `1800`    |
| `disable_animations`       | Disable animations                        | `false`   |

### User Stats Card Options

| Parameter             | Description                                                                                                          | Default   |
|-----------------------|----------------------------------------------------------------------------------------------------------------------|-----------|
| `username`            | GitHub username (required)                                                                                           | -         |
| `hide`                | Comma-separated list of stats to hide (`stars`, `commits`, `prs`, `issues`, `contribs`)                              | -         |
| `show`                | Comma-separated list of extra stats to show (`reviews`, `prs_merged`, `discussions_started`, `discussions_answered`) | -         |
| `hide_rank`           | Hide the ranking circle                                                                                              | `false`   |
| `show_icons`          | Show icons next to stat labels                                                                                       | `false`   |
| `include_all_commits` | Count total commits instead of just last year                                                                        | `false`   |
| `commits_year`        | Count commits for a specific year                                                                                    | -         |
| `number_format`       | Format for numbers (`short`, `long`)                                                                                 | `short`   |
| `text_bold`           | Whether to use bold text                                                                                             | `true`    |
| `exclude_repo`        | Comma-separated list of repositories to exclude                                                                      | -         |
| `line_height`         | Space between rows                                                                                                   | `25`      |
| `card_width`          | Manual card width override                                                                                           | `450`     |
| `theme`               | Card theme                                                                                                           | `default` |
| `hide_border`         | Hide card border                                                                                                     | `false`   |
| `border_radius`       | Card corner radius                                                                                                   | `4.5`     |
| `cache_seconds`       | Custom cache TTL in seconds                                                                                          | `1800`    |
| `disable_animations`  | Disable animations                                                                                                   | `false`   |

### Top Languages Card Options

| Parameter            | Description                                                         | Default       |
|----------------------|---------------------------------------------------------------------|---------------|
| `username`           | GitHub username (required)                                          | -             |
| `hide`               | Comma-separated list of languages to hide                           | -             |
| `layout`             | Card layout (`normal`, `compact`, `donut`, `donut-vertical`, `pie`) | `normal`      |
| `langs_count`        | Number of languages to show (max 20)                                | `5`           |
| `exclude_repo`       | Comma-separated list of repositories to exclude                     | -             |
| `size_weight`        | Weight for code size in ranking                                     | `1`           |
| `count_weight`       | Weight for repo count in ranking                                    | `0`           |
| `hide_progress`      | Hide progress bar/stats in `compact` layout                         | `false`       |
| `stats_format`       | Format for stats (`percentages`, `bytes`)                           | `percentages` |
| `hide_title`         | Hide the card title                                                 | `false`       |
| `custom_title`       | Custom card title                                                   | -             |
| `hide_border`        | Hide the card border                                                | `false`       |
| `border_radius`      | Card corner radius                                                  | `4.5`         |
| `card_width`         | Manual card width override                                          | -             |
| `card_height`        | Manual card height override                                         | -             |
| `theme`              | Card theme                                                          | `default`     |
| `cache_seconds`      | Custom cache TTL in seconds                                         | `1800`        |
| `disable_animations` | Disable animations                                                  | `false`       |

### Themes

Use the `theme` parameter to customize your card:

```markdown
![GitHub Streak](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=tokyonight)
```

#### Theme Examples

| Theme          | Preview                                                                                                                |
|----------------|------------------------------------------------------------------------------------------------------------------------|
| `github_light` | ![github_light](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=github_light) |
| `github_dark`  | ![github_dark](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=github_dark)   |
| `tokyonight`   | ![tokyonight](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=tokyonight)     |
| `dracula`      | ![dracula](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=dracula)           |
| `nord`         | ![nord](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=nord)                 |
| `radical`      | ![radical](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=radical)           |
| `sunset_dark`  | ![sunset_dark](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=sunset_dark)   |
| `ocean_deep`   | ![ocean_deep](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=ocean_deep)     |
| `cyber`        | ![cyber](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=cyber)               |
| `aurora`       | ![aurora](https://github-readme-stats.tuhidulhossain.com/api/streak?username=encryptedtouhid&theme=aurora)             |

#### GitHub Themes

| Theme                        | Description                      |
|------------------------------|----------------------------------|
| `github_light`               | GitHub light mode                |
| `github_light_default`       | GitHub light with gray titles    |
| `github_light_high_contrast` | GitHub light high contrast       |
| `github_light_colorblind`    | GitHub light colorblind-friendly |
| `github_light_tritanopia`    | GitHub light tritanopia-friendly |
| `github_dark`                | GitHub dark mode                 |
| `github_dark_default`        | GitHub dark with white titles    |
| `github_dark_high_contrast`  | GitHub dark high contrast        |
| `github_dark_dimmed`         | GitHub dimmed dark               |
| `github_dark_colorblind`     | GitHub dark colorblind-friendly  |
| `github_dark_tritanopia`     | GitHub dark tritanopia-friendly  |

#### Popular Themes

| Theme              | Description                  |
|--------------------|------------------------------|
| `default`          | Light theme with blue accent |
| `dark`             | Pure dark theme              |
| `radical`          | Pink/purple gradient         |
| `tokyonight`       | Tokyo Night color scheme     |
| `dracula`          | Dracula purple theme         |
| `nord`             | Nord arctic colors           |
| `gruvbox`          | Retro groove colors          |
| `onedark`          | Atom One Dark theme          |
| `catppuccin_mocha` | Catppuccin Mocha             |
| `catppuccin_latte` | Catppuccin Latte             |
| `rose_pine`        | Rose Pine theme              |

#### Editor Themes

| Theme                | Description            |
|----------------------|------------------------|
| `monokai`            | Monokai editor theme   |
| `cobalt`             | Cobalt blue theme      |
| `cobalt2`            | Cobalt2 theme          |
| `nightowl`           | Night Owl editor theme |
| `material-palenight` | Material Palenight     |
| `darcula`            | JetBrains Darcula      |
| `one_dark_pro`       | One Dark Pro           |
| `ayu-mirage`         | Ayu Mirage theme       |
| `noctis_minimus`     | Noctis Minimus         |
| `synthwave`          | 80s synthwave style    |

#### Framework & Brand Themes

| Theme                 | Description            |
|-----------------------|------------------------|
| `vue`                 | Vue.js green theme     |
| `vue-dark`            | Dark Vue.js theme      |
| `react`               | React brand colors     |
| `swift`               | Swift orange theme     |
| `algolia`             | Algolia brand colors   |
| `discord_old_blurple` | Discord old blurple    |
| `buefy`               | Buefy framework colors |

#### Color Palette Themes

| Theme              | Description             |
|--------------------|-------------------------|
| `solarized-dark`   | Solarized dark palette  |
| `solarized-light`  | Solarized light palette |
| `gruvbox_light`    | Light gruvbox variant   |
| `shades-of-purple` | Purple shades theme     |
| `midnight-purple`  | Midnight purple theme   |
| `blue-green`       | Blue-green gradient     |
| `blue_navy`        | Navy blue theme         |
| `calm`             | Calm pastel colors      |
| `calm_pink`        | Calm pink pastel        |
| `rose`             | Rose pink theme         |

#### All Other Themes

| Theme                  | Description                |
|------------------------|----------------------------|
| `default_repocard`     | Light theme for repo cards |
| `merko`                | Green forest theme         |
| `highcontrast`         | High contrast dark         |
| `prussian`             | Prussian blue theme        |
| `great-gatsby`         | Great Gatsby gold          |
| `bear`                 | Bear app theme             |
| `chartreuse-dark`      | Chartreuse dark theme      |
| `gotham`               | Gotham dark theme          |
| `graywhite`            | Gray and white minimal     |
| `vision-friendly-dark` | Accessible dark theme      |
| `flag-india`           | India flag colors          |
| `omni`                 | Omni dark theme            |
| `jolly`                | Jolly bright theme         |
| `maroongold`           | Maroon and gold            |
| `yeblu`                | Yellow and blue            |
| `blueberry`            | Blueberry colors           |
| `slateorange`          | Slate and orange           |
| `kacho_ga`             | Japanese aesthetic         |
| `outrun`               | Outrun retro style         |
| `ocean_dark`           | Deep ocean dark            |
| `city_lights`          | City lights theme          |
| `aura_dark`            | Aura dark theme            |
| `panda`                | Panda syntax theme         |
| `aura`                 | Aura purple theme          |
| `apprentice`           | Apprentice vim theme       |
| `moltack`              | Moltack colors             |
| `codeSTACKr`           | codeSTACKr theme           |
| `date_night`           | Date night romantic        |
| `holi`                 | Holi festival colors       |
| `neon`                 | Cyan/magenta cyberpunk     |
| `ambient_gradient`     | Ambient gradient           |

#### Streak Card Exclusive Themes

Modern themes designed specifically for the streak card:

| Theme           | Description           |
|-----------------|-----------------------|
| `sunset`        | Warm sunset gradient  |
| `sunset_dark`   | Dark sunset variant   |
| `ocean`         | Ocean blue theme      |
| `ocean_deep`    | Deep ocean theme      |
| `forest`        | Forest green theme    |
| `forest_dark`   | Dark forest variant   |
| `purple_wave`   | Purple wave gradient  |
| `purple_galaxy` | Galaxy purple theme   |
| `cyber`         | Cyberpunk neon        |
| `fire`          | Fire red/orange theme |
| `mint`          | Fresh mint green      |
| `coral`         | Coral pink theme      |
| `aurora`        | Purple/teal aurora    |
| `golden`        | Golden luxury theme   |
| `golden_dark`   | Dark gold variant     |
| `rose_gold`     | Rose gold elegant     |
| `electric`      | Electric blue theme   |
| `lavender`      | Soft lavender         |
| `arctic`        | Arctic ice blue       |

## Configuration

Configure via `appsettings.json` or environment variables:

- **GitHub API** - Token and endpoint settings
- **Redis** - Connection string for distributed caching
- **Cache TTL** - Customizable cache duration per card type
- **Access Control** - Whitelist/blacklist for users and repositories

## License

MIT
