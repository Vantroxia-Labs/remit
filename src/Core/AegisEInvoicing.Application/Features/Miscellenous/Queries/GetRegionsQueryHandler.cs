using AegisEInvoicing.Application.DTOs;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public class GetRegionsQueryHandler : IRequestHandler<GetRegionsQuery, List<RegionDto>>
{
    public async Task<List<RegionDto>> Handle(GetRegionsQuery request, CancellationToken cancellationToken)
    {
        // Read states from JSON file
        var states = await GetStatesFromJson();

        // Group states by region
        var regionGroups = states
            .Where(s => !string.IsNullOrEmpty(s.region))
            .GroupBy(s => s.region)
            .ToList();

        var regions = new List<RegionDto>();

        // Build regions from grouped data
        foreach (var group in regionGroups)
        {
            if (Enum.TryParse<NigerianRegion>(group.Key, out var regionEnum))
            {
                var regionDto = new RegionDto
                {
                    Id = (int)regionEnum,
                    Name = FormatRegionName(regionEnum),
                    Code = GetRegionCode(regionEnum),
                    States = group.Select(s => s.name!).ToList()
                };
                regions.Add(regionDto);
            }
        }

        // Add "Others" region for international/non-Nigerian states
        regions.Add(new RegionDto
        {
            Id = (int)NigerianRegion.Others,
            Name = NigerianRegion.Others.ToString(),
            Code = "OT",
            States = null
        });

        // Sort by Id to maintain consistent order
        return regions.OrderBy(r => r.Id).ToList();
    }

    private static async Task<List<StateCityDto>> GetStatesFromJson()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string completeFilePath = Path.Combine(currentDirectory, "states-and-cities.json");
        string jsonString = await File.ReadAllTextAsync(completeFilePath);
        var states = JsonConvert.DeserializeObject<List<StateCityDto>>(jsonString);
        return states ?? new List<StateCityDto>();
    }

    private static string FormatRegionName(NigerianRegion region)
    {
        // Convert enum to string and add hyphen before capital letters (except first)
        // e.g., "NorthCentral" -> "North-Central"
        return Regex.Replace(region.ToString(), "(?<!^)([A-Z])", "-$1");
    }

    private static string GetRegionCode(NigerianRegion region)
    {
        return region switch
        {
            NigerianRegion.NorthCentral => "NC",
            NigerianRegion.NorthEast => "NE",
            NigerianRegion.NorthWest => "NW",
            NigerianRegion.SouthEast => "SE",
            NigerianRegion.SouthSouth => "SS",
            NigerianRegion.SouthWest => "SW",
            NigerianRegion.Others => "OT",
            _ => ""
        };
    }
}
