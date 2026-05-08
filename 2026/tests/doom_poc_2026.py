import math

import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList
from System.Threading import Thread


def log(msg):
    cad.Msg("[DOOM POC 2026] " + msg)


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
            return d, x, y, True
        if grid[gy][gx] == "#":
            return d, x, y, True
        d += step
    return max_dist, px + math.cos(ang) * max_dist, py + math.sin(ang) * max_dist, False


log("Avvio DOOM-like POC (raycasting 2.5D)")
res = cad.GetPoint("Punto base DOOM POC:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    base_x = p.X
    base_y = p.Y
    base_z = p.Z

    # Mappa semplice (10x10), bordo pieno + ostacoli interni.
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

    # Layout scene:
    # - sinistra: minimappa top-down
    # - destra: "viewport" fake 3D con colonne verticali
    map_origin_x = base_x
    map_origin_y = base_y
    view_origin_x = base_x + map_w * cell + 35.0
    view_origin_y = base_y

    static_ids = []
    dyn_ids = []

    # Disegna minimappa statica (muri)
    for gy in range(map_h):
        for gx in range(map_w):
            if grid[gy][gx] != "#":
                continue
            x0 = map_origin_x + gx * cell
            y0 = map_origin_y + gy * cell
            x1 = x0 + cell
            y1 = y0 + cell
            static_ids.append(cad.AddLine(x0, y0, base_z, x1, y0, base_z))
            static_ids.append(cad.AddLine(x1, y0, base_z, x1, y1, base_z))
            static_ids.append(cad.AddLine(x1, y1, base_z, x0, y1, base_z))
            static_ids.append(cad.AddLine(x0, y1, base_z, x0, y0, base_z))

    # Cornice viewport fake 3D
    vw = 180.0
    vh = map_h * cell
    static_ids.append(cad.AddLine(view_origin_x, view_origin_y, base_z, view_origin_x + vw, view_origin_y, base_z))
    static_ids.append(cad.AddLine(view_origin_x + vw, view_origin_y, base_z, view_origin_x + vw, view_origin_y + vh, base_z))
    static_ids.append(cad.AddLine(view_origin_x + vw, view_origin_y + vh, base_z, view_origin_x, view_origin_y + vh, base_z))
    static_ids.append(cad.AddLine(view_origin_x, view_origin_y + vh, base_z, view_origin_x, view_origin_y, base_z))

    # Parametri player/camera
    px = 2.5
    py = 2.5
    pa = 0.20
    fov = math.radians(70.0)
    rays = 48
    max_dist = 16.0
    ray_step = 0.05

    # Script movimento (frame-by-frame)
    moves = [
        (0.22, 0.00),
        (0.22, 0.00),
        (0.22, 0.03),
        (0.20, 0.03),
        (0.18, 0.04),
        (0.16, 0.05),
        (0.14, 0.06),
        (0.12, 0.06),
        (0.10, 0.05),
        (0.08, 0.04),
        (0.06, 0.03),
        (0.05, 0.02),
        (0.05, -0.02),
        (0.06, -0.03),
        (0.08, -0.04),
        (0.10, -0.05),
        (0.12, -0.06),
        (0.14, -0.06),
        (0.16, -0.05),
        (0.18, -0.04),
        (0.20, -0.03),
        (0.22, -0.02),
    ]

    log("Render in corso: {} frame".format(len(moves)))

    for i in range(len(moves)):
        forward, turn = moves[i]
        pa += turn
        nx = px + math.cos(pa) * forward
        ny = py + math.sin(pa) * forward
        if grid[int(math.floor(ny))][int(math.floor(nx))] != "#":
            px = nx
            py = ny

        safe_erase(dyn_ids)
        dyn_ids = []

        # Player marker su minimappa
        pmx = map_origin_x + px * cell
        pmy = map_origin_y + py * cell
        dyn_ids.append(cad.AddCircle(pmx, pmy, base_z, 1.2))
        dyn_ids.append(cad.AddLine(pmx, pmy, base_z, pmx + math.cos(pa) * 4.0, pmy + math.sin(pa) * 4.0, base_z))

        # Raycasting + rendering 2D/3D fake
        col_w = vw / float(rays)
        for r in range(rays):
            t = (float(r) / float(rays - 1)) - 0.5
            ra = pa + t * fov
            dist, hx, hy, hit = cast_ray(grid, px, py, ra, max_dist, ray_step)

            # Minimappa rays
            rx = map_origin_x + hx * cell
            ry = map_origin_y + hy * cell
            dyn_ids.append(cad.AddLine(pmx, pmy, base_z, rx, ry, base_z))

            # Correzione fish-eye + altezza colonna
            corr = dist * math.cos(ra - pa)
            if corr < 0.001:
                corr = 0.001
            slice_h = min(vh - 2.0, (vh * 0.9) / corr)
            cx = view_origin_x + (r + 0.5) * col_w
            cy_mid = view_origin_y + vh * 0.5
            y1 = cy_mid - slice_h * 0.5
            y2 = cy_mid + slice_h * 0.5
            dyn_ids.append(cad.AddLine(cx, y1, base_z, cx, y2, base_z))

        cad.RegenSafe()
        Thread.Sleep(70)

    log("POC completato. Entita statiche={0}, ultimo frame entita dinamiche={1}".format(len(static_ids), len(dyn_ids)))
    log("Marker=" + cad.GetBuildMarker())
