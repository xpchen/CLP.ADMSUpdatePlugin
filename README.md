# CLP.ADMSUpdatePlugin

ArcGIS Pro add-in for updating **ADMS Name**, **ADMS Alias**, and **SOM** fields on Utility Network features (HV and LV).

## Requirements

- ArcGIS Pro 3.3+
- .NET 8 SDK
- Utility Network map with Electric domain (HV / LV tiers)

## Usage

1. Open a map with a Utility Network layer.
2. Click **Update ADMS** on the Add-In tab.
3. Choose an update mode, select feature(s) on the map, then **Next Step**.
4. Review preview values (where available) and click **Update**.

## Update modes

| Mode | Description |
|------|-------------|
| Update SS To SS | Trace between two HV substations via cable/connector; update switches, bus, cables |
| Update Spare CB | Single spare circuit breaker at a substation |
| Manual Update Pole Feature | Pole/substation devices: Isolator, Fuse, Switch, HV PM TX, Subring CB |
| Multiple Update Pole Cable/OHL | Bulk ADMS update for selected cables/overhead lines |
| Update LV Feature | Source Fuse, Supply Point, Pillar Fuse, Link Box, Mother Supply Point |

## Documentation

- **[Project logic & architecture](docs/PROJECT.md)** — full workflows, models, formulas, file map
- **[Pole enhancement plan](.cursor/plans/pole-function-enhancement.plan.md)** — Pole mode status and debug reference

## Project structure

```
CLP.ADMSUpdatePlugin/
├── ADMSUpdateDockpane.xaml          # UI
├── ADMSUpdateDockpaneViewModel.cs   # Core logic
├── ADMSUpdateHelper.cs              # ADMS naming formulas
├── Pole_Model.cs / SS_TO_SS_Model.cs / LVFeature_Model.cs
├── SpatialSubgraphExtractor.cs      # Trace → FeatureSnapshot
├── TraceCfgHelpers.cs
├── Config.daml
└── docs/PROJECT.md                  # Detailed documentation
```

## Build

Open `CLP.ADMSUpdatePlugin.csproj` in Visual Studio with ArcGIS Pro SDK installed, or:

```bash
dotnet build CLP.ADMSUpdatePlugin.csproj
```
