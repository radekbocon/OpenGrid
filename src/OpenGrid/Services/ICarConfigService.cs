using System;
using System.Collections.Generic;
using OpenGrid.Models;

namespace OpenGrid.Services;

public interface ICarConfigService
{
    IReadOnlyList<CarProfile> GetAll();
    CarProfile? GetById(string id);
    CarProfile? GetByCarKey(string carKey);
    void Save(CarProfile car);
    void Remove(string id);
    event EventHandler<CarProfile>? CarAdded;
    event EventHandler<CarProfile>? CarRemoved;
    event EventHandler<CarProfile>? CarUpdated;
}
