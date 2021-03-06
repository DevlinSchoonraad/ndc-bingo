using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NdcBingo.Data;
using NdcBingo.Game;
using NdcBingo.Models.Game;
using NdcBingo.Services;

namespace NdcBingo.Controllers
{
    [Route("game")]
    public class GameController : Controller
    {
        private readonly IPlayerData _playerData;
        private readonly ISquareData _squareData;
        private readonly IDataCookies _dataCookies;

        public GameController(ISquareData squareData, IDataCookies dataCookies, IPlayerData playerData)
        {
            _squareData = squareData;
            _dataCookies = dataCookies;
            _playerData = playerData;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Play()
        {
            if (!_dataCookies.TryGetPlayerCode(out var code))
            {
                return RedirectToAction("New", "Players", new {returnUrl = Url.Action("Play")});
            }

            var player = await _playerData.Get(code);
            if (player == null)
            {
                return RedirectToAction("New", "Players", new {returnUrl = Url.Action("Play")});
            }
            
            var vm = await CreateGameViewModel();
            vm.ColumnCount = Constants.SquaresPerLine;

            vm.PlayerName = player.Name;
            
            return View(vm);
        }

        private async Task<GameViewModel> CreateGameViewModel()
        {
            GameViewModel vm;
            if (_dataCookies.TryGetPlayerSquares(out var squareIds))
            {
                vm = await CreateGameInProgressViewModel(squareIds);
            }
            else
            {
                vm = await CreateNewGameViewModel();
            }

            foreach (var square in vm.Squares.Where(s => !s.Claimed))
            {
                square.ClaimLink = Url.Action("Claim", new {squareId = square.Id});
            }

            return vm;
        }

        [HttpGet("claim/{squareId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Claim(int squareId)
        {
            if (_dataCookies.TryGetPlayerSquares(out var squareIds))
            {
                int index = Array.IndexOf(squareIds, squareId);
                if (index > -1)
                {
                    if (!_dataCookies.TryGetPlayerClaims(out var claims))
                    {
                        claims = new int[Constants.SquareCount];
                    }

                    claims[index] = 1;
                    _dataCookies.SetPlayerClaims(claims);
                }
            }

            return RedirectToAction("Play");
        }

        private async Task<GameViewModel> CreateNewGameViewModel()
        {
            GameViewModel vm;
            var squares = await _squareData.GetRandomSquaresAsync(Constants.SquareCount);
            vm = new GameViewModel
            {
                Squares = squares.Select(s => new SquareViewModel(s.Id, s.Text, s.Type, s.Description)).ToArray(),
                Claims = new int[Constants.SquareCount]
            };
            _dataCookies.SetPlayerSquares(squares);
            _dataCookies.SetPlayerClaims(new int[Constants.SquareCount]);
            return vm;
        }

        private async Task<GameViewModel> CreateGameInProgressViewModel(int[] squareIds)
        {
            var squares = await _squareData.GetSquaresAsync(squareIds);
            var vm = new GameViewModel
            {
                Squares = squares.Select(s => new SquareViewModel(s.Id, s.Text, s.Type, s.Description)).ToArray()
            };
            if (_dataCookies.TryGetPlayerClaims(out var claims))
            {
                for (int i = 0, l = Math.Min(vm.Squares.Length, claims.Length); i < l; i++)
                {
                    vm.Squares[i].Claimed = claims[i] > 0;
                }

                var winningLines = WinCondition.Check(claims);
                vm.WinningLines = winningLines.Horizontal + winningLines.Vertical + winningLines.Diagonal;
            }

            return vm;
        }
    }
}