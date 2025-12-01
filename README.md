# Battleships Game API

A classic naval combat game for two players built with .NET Web API backend and Blazor frontend.

## Game Modes

### 🖥️ Local Game (Single Computer)
Play with a friend on the same computer! Players take turns, with a turn transition screen to prevent cheating.

### 🌐 Network Game (Two Computers)
Create or join games over the network. One player creates a game and waits, the other joins.

## Game Rules

- **2 Players** take turns shooting at each other's boards
- **Board size**: 10-20 squares (configurable)
- **Ships are placed randomly** for each player
- **Ships per player**:
  - 2x Single (1 cell)
  - 2x Double (2 cells)
  - 1x Triple (3 cells)
  - 1x Cross-shaped (5 cells)
  - 1x Plus-shaped (5 cells)

## Shot Results

- **Voda** (Water) - Miss
- **Zásah** (Hit) - Hit a ship
- **Potopena celá** (Sunk) - Ship is completely sunk

## Project Structure

- `BattleshipsGame.Api` - .NET Web API backend
- `BattleshipsGame.Client` - Blazor Server frontend
- `BattleshipsGame.Tests` - xUnit tests

## Getting Started

### Prerequisites

- .NET 9.0 SDK

### Running the API

```bash
cd BattleshipsGame.Api
dotnet run
```

The API will be available at `http://localhost:5000`

### Running the Blazor Client

```bash
cd BattleshipsGame.Client
dotnet run
```

The client will be available at `http://localhost:5001`

### Running Tests

```bash
dotnet test
```

## API Endpoints

### Network Game Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/game/create` | Create a new network game |
| POST | `/api/game/{gameId}/join` | Join an existing game |
| POST | `/api/game/{gameId}/shot` | Make a shot (requires X-Player-Id header) |
| GET | `/api/game/{gameId}/status` | Get game status (requires X-Player-Id header) |
| GET | `/api/game/available` | Get list of available games |

### Local Game Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/game/local/create` | Create a new local game (both players auto-created) |
| POST | `/api/game/local/{gameId}/shot` | Make a shot (uses current player's turn) |
| GET | `/api/game/local/{gameId}/status` | Get local game status |

## Request/Response Examples

### Create Network Game

```json
POST /api/game/create
{
  "boardSize": 10
}

Response:
{
  "gameId": "guid",
  "playerId": "guid",
  "boardSize": 10
}
```

### Create Local Game

```json
POST /api/game/local/create
{
  "boardSize": 10
}

Response:
{
  "gameId": "guid",
  "player1Id": "guid",
  "player2Id": "guid",
  "boardSize": 10,
  "currentPlayerId": "guid"
}
```

### Make Shot (Network Game)

```json
POST /api/game/{gameId}/shot
Header: X-Player-Id: {playerId}
{
  "x": 5,
  "y": 3
}

Response:
{
  "result": "Water" | "Hit" | "Sunk",
  "gameOver": false,
  "winnerId": null,
  "shipTypeSunk": null
}
```

### Make Shot (Local Game)

```json
POST /api/game/local/{gameId}/shot
{
  "x": 5,
  "y": 3
}

Response:
{
  "result": "Water" | "Hit" | "Sunk",
  "gameOver": false,
  "winnerId": null,
  "shipTypeSunk": null
}
```