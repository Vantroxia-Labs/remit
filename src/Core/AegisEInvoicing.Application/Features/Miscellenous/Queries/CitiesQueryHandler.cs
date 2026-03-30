using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.DTOs;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using MediatR;
using Newtonsoft.Json;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public class CitiesQueryHandler : IRequestHandler<CitiesQuery, PaginatedList<CitiesSummaryDto>>
{

    public async Task<PaginatedList<CitiesSummaryDto>> Handle(CitiesQuery request, CancellationToken cancellationToken)
    {

        var cities = await GetCities(request.stateName);

        
        return new PaginatedList<CitiesSummaryDto>(cities, cities.Count, 1, 1);
    }

    public async Task<List<CitiesSummaryDto>> GetCities(string stateName)
    {
        List<CitiesSummaryDto>? citiesSummaryDtos = new List<CitiesSummaryDto>();
        string currentDirectory = Directory.GetCurrentDirectory();
        string completeFilePath = currentDirectory + "\\states-and-cities.json";
        string jsonString = File.ReadAllText(completeFilePath);
        var state_cities = JsonConvert.DeserializeObject<List<StateCityDto>>(jsonString);

        var cities = state_cities!.Where(s => s.name == stateName).FirstOrDefault();     

        citiesSummaryDtos = cities?.cities?.Select(x => new CitiesSummaryDto()
        {
            Name = x
        }).ToList();

        return await Task.FromResult(citiesSummaryDtos ?? new List<CitiesSummaryDto>());
    }
}
