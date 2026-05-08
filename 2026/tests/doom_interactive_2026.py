import math

import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus, PromptStringOptions
from System.Collections import ArrayList


def log(msg):
    cad.Msg("[DOOM INTERACTIVE 2026] " + msg)


def safe_erase(ids):
    if ids is None or len(ids) == 0:
        return
    try:
        cad.EraseEntities(ArrayList(ids))
    except:
        pass


def cast_ray(grid, px, py, ang, max_dist, step):
    d = 0.0
    h = len(grid)
    w = len(grid[0]) if h > 0 else 0
    while d <= max_dist:
        x = px + math.cos(ang) * d
        y = py + math.sin(ang) * d
        gx = int(math.floor(x))
        gy = int(math.floor(y))
        if gx < 0 or gy < 0 or gx >= w or gy >= h:
            return d, x, y
        if grid[gy][gx] == "#":
            return d, x, y
        d += step
    return max_dist, px + math.cos(ang) * max_dist, py + math.sin(ang) * max_dist


def render_frame(px, py, pa):
    safe_erase(render_frame.dyn_ids)
    render_frame.dyn_ids = []

    pmx = render_frame.map_origin_x + px * render_frame.cell
    pmy = render_frame.map_origin_y + py * render_frame.cell
    render_frame.dyn_ids.append(cad.AddCircle(pmx, pmy, render_frame.base_z, 1.2))
    render_frame.dyn_ids.append(cad.AddLine(pmx, pmy, render_frame.base_z, pmx + math.cos(pa) * 4.0, pmy + math.sin(pa) * 4.0, render_frame.base_z))

    col_w = render_frame.vw / float(render_frame.rays)
    for r in range(render_frame.rays):
        t = (float(r) / float(render_frame.rays - 1)) - 0.5
        ra = pa + t * render_frame.fov
        dist, hx, hy = cast_ray(render_frame.grid, px, py, ra, render_frame.max_dist, render_frame.ray_step)

        rx = render_frame.map_origin_x + hx * render_frame.cell
        ry = render_frame.map_origin_y + hy * render_frame.cell
        render_frame.dyn_ids.append(cad.AddLine(pmx, pmy, render_frame.base_z, rx, ry, render_frame.base_z))

        corr = dist * math.cos(ra - pa)
        if corr < 0.001:
            corr = 0.001
        slice_h = min(render_frame.vh - 2.0, (render_frame.vh * 0.9) / corr)
        cx = render_frame.view_origin_x + (r + 0.5) * col_w
        cy_mid = render_frame.view_origin_y + render_frame.vh * 0.5
        y1 = cy_mid - slice_h * 0.5
        y2 = cy_mid + slice_h * 0.5
        render_frame.dyn_ids.append(cad.AddLine(cx, y1, render_frame.base_z, cx, y2, render_frame.base_z))

    cad.RegenSafe()


render_frame.dyn_ids = []

log("Avvio DOOM interattivo (turn-based). Comandi: W A S D, Q/E strafe, X esci.")
res = cad.GetPoint("Punto base DOOM interattivo:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    map_rows = [
        "##########",
        "#........#",
        "#..##....#",
        "#........#",
        "#....#...#",
        "#........#",
        "#..#.....#",
        "#........#",
        "#.....#..#",
        "##########",
    ]
    grid = [list(r) for r in map_rows]

    cell = 8.0
    map_w = len(grid[0])
    map_h = len(grid)

    map_origin_x = p.X
    map_origin_y = p.Y
    view_origin_x = p.X + map_w * cell + 35.0
    view_origin_y = p.Y
    vw = 180.0
    vh = map_h * cell

    static_ids = []
    for gy in range(map_h):
        for gx in range(map_w):
            if grid[gy][gx] != "#":
                continue
            x0 = map_origin_x + gx * cell
            y0 = map_origin_y + gy * cell
            x1 = x0 + cell
            y1 = y0 + cell
            static_ids.append(cad.AddLine(x0, y0, p.Z, x1, y0, p.Z))
            static_ids.append(cad.AddLine(x1, y0, p.Z, x1, y1, p.Z))
            static_ids.append(cad.AddLine(x1, y1, p.Z, x0, y1, p.Z))
            static_ids.append(cad.AddLine(x0, y1, p.Z, x0, y0, p.Z))

    static_ids.append(cad.AddLine(view_origin_x, view_origin_y, p.Z, view_origin_x + vw, view_origin_y, p.Z))
    static_ids.append(cad.AddLine(view_origin_x + vw, view_origin_y, p.Z, view_origin_x + vw, view_origin_y + vh, p.Z))
    static_ids.append(cad.AddLine(view_origin_x + vw, view_origin_y + vh, p.Z, view_origin_x, view_origin_y + vh, p.Z))
    static_ids.append(cad.AddLine(view_origin_x, view_origin_y + vh, p.Z, view_origin_x, view_origin_y, p.Z))

    render_frame.grid = grid
    render_frame.cell = cell
    render_frame.map_origin_x = map_origin_x
    render_frame.map_origin_y = map_origin_y
    render_frame.view_origin_x = view_origin_x
    render_frame.view_origin_y = view_origin_y
    render_frame.vw = vw
    render_frame.vh = vh
    render_frame.base_z = p.Z
    render_frame.fov = math.radians(70.0)
    render_frame.rays = 56
    render_frame.max_dist = 16.0
    render_frame.ray_step = 0.05

    px = 2.5
    py = 2.5
    pa = 0.20

    render_frame(px, py, pa)

    while True:
        pso = PromptStringOptions("\nDOOM cmd [W/A/S/D Q/E X]: ")
        pso.AllowSpaces = False
        pr = ed.GetString(pso)
        if pr.Status != PromptStatus.OK:
            break
        cmd = (pr.StringResult or "").strip().upper()
        if cmd == "":
            continue
        if cmd == "X":
            break

        move = 0.35
        turn = math.radians(10.0)
        nx, ny, na = px, py, pa

        if cmd == "A":
            na -= turn
        elif cmd == "D":
            na += turn
        elif cmd == "W":
            nx = px + math.cos(pa) * move
            ny = py + math.sin(pa) * move
        elif cmd == "S":
            nx = px - math.cos(pa) * move
            ny = py - math.sin(pa) * move
        elif cmd == "Q":
            nx = px - math.sin(pa) * move
            ny = py + math.cos(pa) * move
        elif cmd == "E":
            nx = px + math.sin(pa) * move
            ny = py - math.cos(pa) * move

        gx = int(math.floor(nx))
        gy = int(math.floor(ny))
        if gx >= 0 and gy >= 0 and gy < len(grid) and gx < len(grid[0]) and grid[gy][gx] != "#":
            px, py = nx, ny
        pa = na
        render_frame(px, py, pa)

    safe_erase(render_frame.dyn_ids)
    log("Sessione chiusa. Statiche create={0}".format(len(static_ids)))
    log("Marker=" + cad.GetBuildMarker())
