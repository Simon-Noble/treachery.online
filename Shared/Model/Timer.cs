﻿/*
 * Copyright (C) 2020-2024 Ronald Ossendrijver (admin@treachery.online)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. This
 * program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. You should have
 * received a copy of the GNU General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Treachery.Shared;

public class Timer<T>
{
    private readonly Dictionary<T, TimeSpan> _times = new();

    public TimeSpan TimeSpent(T timedItem)
    {
        if (_times.TryGetValue(timedItem, out var ts))
            return ts;
        return TimeSpan.Zero;
    }

    public void Add(T timedItem, TimeSpan ts)
    {
        if (_times.ContainsKey(timedItem))
            _times[timedItem] += ts;
        else
            _times.Add(timedItem, ts);
    }
}