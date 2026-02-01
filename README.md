# MISA

MISA is a personal meta-organization and orchestration system.
Its goal is to make work, knowledge, and decisions clearly defined, traceable, and adaptable over time.

Instead of managing isolated tasks or notes, MISA focuses on how things influence one another, move through states, and cause further changes.

## Concept

All work and knowledge are represented as Items with defined lifecycles, states, and relationships. An item can schedule other items, trigger actions, and produce effects that can in turn lead to new triggers.

Scheduling is not limited to time-based execution. A schedule can create further schedules, attach actions, and cause follow-up effects, forming a recursive automation model with a small and stable core.

From these basic building blocks—items, schedules, triggers, actions, and effects—more complex behavior can emerge without adding special cases.

## Direction

MISA is evolving toward a system for knowledge and work that can be examined and questioned, where processes are visible, decisions can be revisited, and automation stays understandable as it grows.

It also draws from Zettelkasten-style linking, keeps sources explicit, and integrates external inputs such as RSS feeds, so ideas and references can be followed back to where they came from and connected over time.

## Tech Stack
• .NET  
• ASP.NET Web API  
• Avalonia UI (MVVM)  

• PostgreSQL  
• Entity Framework Core  

• REST  
• SignalR 

• Wolverine  
• RabbitMQ  
