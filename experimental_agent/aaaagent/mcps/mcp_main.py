from pathlib import Path

from . import hitl
from . import terminal_controller

from .schema import MCPS


def get_mcps_configs(mcps: list[MCPS]):

    config = {}

    for mcp in mcps:
        match mcp:

            case MCPS.Terminal:
                config["terminal-controller"] = {
                    "transport": "stdio",
                    "command": "python",
                    "args": [Path(terminal_controller.__file__).absolute().__str__()],
                }

            case MCPS.HITL:
                config["human-in-the-loop"] = {
                    "transport": "stdio",
                    "command": "python",
                    "args": [Path(hitl.__file__).absolute().__str__()],
                }

    return config