using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

public static class FeatureQueryHelper
{
    public static FeatureLayer GetFeatureLayer(string layerName)
    {
        return MapView.Active?.Map
            .GetLayersAsFlattenedList()
            .OfType<FeatureLayer>()
            .FirstOrDefault(l => l.Name == layerName);
    }

    public static FeatureSnapshot QueryFeatureSnapshotByGlobalId(UtilityNetwork utilityNetwork, string layerName, Guid globalId)
    {
        var layer = GetFeatureLayer(layerName);
        if (layer == null) return null;

        var queryFilter = new QueryFilter { WhereClause = "GLOBALID = '{" + globalId + "}'" };
        using var cursor = layer.GetFeatureClass().Search(queryFilter);
        if (!cursor.MoveNext()) return null;

        var element = utilityNetwork.CreateElement(cursor.Current);
        var results = new SpatialSubgraphExtractor(utilityNetwork).Extract(new List<Element>() { element });
        return results.FeatureByGlobalId.Values.FirstOrDefault(p => p.Element.GlobalID == globalId);
    }

    public static Row QueryRowByGlobalId(string layerName, Guid globalId)
    {
        var layer = GetFeatureLayer(layerName);
        if (layer == null) return null;

        var queryFilter = new QueryFilter { WhereClause = "GLOBALID = '{" + globalId + "}'" };
        using var cursor = layer.GetFeatureClass().Search(queryFilter);
        return cursor.MoveNext() ? cursor.Current : null;
    }
}
