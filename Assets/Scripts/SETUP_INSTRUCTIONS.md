# VR Interaction Prototype - Setup Instructions

## Descriere Generală
Acest set de scripturi implementează un **blocky prototype** pentru o aplicație VR cu funcționalități de bază pentru grab, click, și plasare obiecte.

## Scripturi Create

### 1. **GrabbableObject.cs**
Script pentru obiecte care pot fi prinse (grab) în VR.
- **Funcționalitate**: Grab & Release
- **Visual Feedback**: Schimbă culoarea la hover (galben) și grab (verde)
- **Componente necesare**: XRGrabInteractable, Rigidbody, Renderer

### 2. **PlacementZone.cs**
Script pentru zone unde obiectele pot fi plasate.
- **Funcționalitate**: Detectează când obiecte grababile intră în zonă
- **Visual Feedback**: 
  - Gri = Idle
  - Cyan = Obiect valid în apropierea zonei
  - Verde = Obiect plasat cu succes
- **Features**: Snap to center (opțional)

### 3. **InteractionManager.cs**
Manager central pentru gestionarea logicii jocului.
- **Funcționalitate**: Monitorizează progresul și win conditions
- **Features**: Reset game, tracking acțiuni complete

### 4. **SimpleClickable.cs**
Script pentru butoane și obiecte clickable.
- **Funcționalitate**: Click/Activate interactions
- **Modes**: Toggle sau momentary click
- **Use case**: Reset button, toggle objects on/off

### 5. **VisualFeedbackHelper.cs**
Helper pentru feedback vizual și texturi.
- **Funcționalitate**: Aplicare culori simple sau checkerboard textures
- **Purpose**: Creează graybox environment clar și ușor de citit

---

## Setup în Unity

### Pas 1: Setup Obiectele Interactive (Cube și Sphere)

#### Pentru CUBE:
1. Create > 3D Object > Cube
2. Add Component: **GrabbableObject**
3. Add Component: **XR Grab Interactable** (din XR Interaction Toolkit)
4. Add Component: **Rigidbody**
   - Mass: 1
   - Use Gravity: true
5. Setează Tag-ul la "Cube" (Tag Manager)
6. În GrabbableObject:
   - Object Name: "Cube"
   - Normal Color: Alb
   - Hover Color: Galben
   - Grabbed Color: Verde
7. Add Component: **VisualFeedbackHelper** (opțional)
   - Use Simple Color: true
   - Simple Color: Albastru deschis (#ADD8E6)

#### Pentru SPHERE:
1. Create > 3D Object > Sphere
2. Repetă pașii de la Cube
3. Setează Tag-ul la "Sphere"
4. În GrabbableObject:
   - Object Name: "Sphere"
5. În VisualFeedbackHelper:
   - Simple Color: Portocaliu (#FFA500)

### Pas 2: Setup Placement Zones

#### Cube Zone:
1. Create > 3D Object > Cube (acest obiect va fi zona)
2. Scalează: (2, 0.2, 2) - face o platformă plată
3. Add Component: **PlacementZone**
4. În PlacementZone:
   - Zone Name: "Cube Zone"
   - Accepted Tags: Array size 1, Element 0: "Cube"
   - Snap To Center: true
5. Add Component: **VisualFeedbackHelper**
   - Apply Checkerboard: true
   - Color 1: Gri deschis
   - Color 2: Gri închis
6. Poziționează-l în scenă unde vrei

#### Sphere Zone:
1. Repetă pașii de mai sus
2. În PlacementZone:
   - Zone Name: "Sphere Zone"
   - Accepted Tags: "Sphere"
3. Poziționează-l separat de Cube Zone

### Pas 3: Setup Interaction Manager

1. Create Empty GameObject, numește-l "InteractionManager"
2. Add Component: **InteractionManager**
3. În Inspector:
   - Cube Zone: Drag & drop Cube Zone object
   - Sphere Zone: Drag & drop Sphere Zone object
   - Cube Spawn Point: Create Empty GameObject ca spawn point pentru cube
   - Sphere Spawn Point: Create Empty GameObject ca spawn point pentru sphere

### Pas 4: Setup Reset Button (Opțional)

1. Create > 3D Object > Cube (sau Cylinder pentru un buton mai realistic)
2. Scalează-l mic: (0.3, 0.1, 0.3)
3. Add Component: **SimpleClickable**
4. Add Component: **XR Simple Interactable**
5. În SimpleClickable:
   - Button Name: "Reset Button"
   - Normal Color: Albastru
   - Clicked Color: Roșu
   - Toggle Action: false (pentru click momentan)
6. Add Component: **VisualFeedbackHelper**
   - Simple Color: Roșu (#FF0000)

### Pas 5: Setup Environment (Graybox)

#### Ground/Floor:
1. Create > 3D Object > Plane
2. Scale: (10, 1, 10)
3. Add Component: **VisualFeedbackHelper**
   - Apply Checkerboard: true
   - Color 1: #E0E0E0 (gri foarte deschis)
   - Color 2: #C0C0C0 (gri deschis)
   - Checker Size: 8

#### Walls (Opțional):
1. Create > 3D Object > Cube
2. Scale pentru a face un perete: (10, 3, 0.2)
3. Add Component: **VisualFeedbackHelper**
   - Simple Color: #D3D3D3 (gri mediu)

### Pas 6: Setup XR Rig

1. Asigură-te că ai un **XR Origin (XR Rig)** în scenă
2. Verifică că are:
   - Main Camera
   - Left Controller (cu XR Controller și XR Ray Interactor)
   - Right Controller (cu XR Controller și XR Ray Interactor)
3. Toate acestea ar trebui să fie incluse automat dacă folosești XR Interaction Toolkit

---

## Paletă de Culori Recomandată (Graybox + Highlights)

### Environment (Non-interactive):
- Ground: Checkerboard alb-gri (#FFFFFF / #CCCCCC)
- Walls: Gri mediu (#B0B0B0)
- Ceiling: Gri deschis (#D0D0D0)

### Interactive Objects:
- Cube: Albastru deschis (#87CEEB)
- Sphere: Portocaliu (#FFA500)
- Placement Zones: Checkerboard gri (#888888 / #666666)

### Feedback Colors:
- Hover: Galben (#FFFF00)
- Grabbed: Verde (#00FF00)
- Zone Highlight: Cyan (#00FFFF)
- Success: Verde intens (#00CC00)
- Buttons: Roșu (#FF0000) sau Albastru (#0000FF)

---

## Testare

### Acțiuni de testat:
1. ✓ **Grab Cube**: Apropie controller-ul de cube, ar trebui să devină galben (hover), apoi apasă grip pentru grab (verde)
2. ✓ **Place Cube**: Eliberează cube-ul deasupra Cube Zone - ar trebui să snappuiască la centru și zona să devină verde
3. ✓ **Grab Sphere**: Repetă procesul pentru sphere
4. ✓ **Place Sphere**: Eliberează sphere-ul în Sphere Zone
5. ✓ **Success Condition**: Când ambele sunt plasate, jocul înregistrează succes în Console
6. ✓ **Reset**: Apasă butonul de reset pentru a reporni jocul

### Verificări în Console:
- Vezi mesaje când obiectele sunt grabbed/released
- Vezi mesaje când obiectele intră/ies din zone
- Vezi confirmarea de succes când toate obiectele sunt plasate

---

## Extensii Posibile

1. **Mai multe tipuri de obiecte**: Adaugă cilindri, capsule etc.
2. **Puzzle logic**: Obiecte trebuie plasate într-o ordine specifică
3. **Timer**: Adaugă un countdown pentru challenge
4. **Score system**: Puncte pentru plasare corectă
5. **Sound effects**: Audio feedback pentru interacțiuni
6. **Physics puzzles**: Obiecte care trebuie să declanșeze mecanisme
7. **UI Display**: Canvas 3D cu progress și instructions

---

## Troubleshooting

### Obiectele nu pot fi prinse:
- Verifică că ai XR Grab Interactable pe obiect
- Verifică că ai XR Ray Interactor sau XR Direct Interactor pe controller
- Verifică Interaction Layer Mask

### Placement Zone nu funcționează:
- Verifică că Collider-ul este setat ca **Trigger**
- Verifică că tag-urile obiectelor sunt corecte
- Verifică că Accepted Tags în PlacementZone match-uiește tag-urile obiectelor

### Culorile nu se schimbă:
- Verifică că obiectul are un Renderer
- Verifică că material-ul suportă schimbarea culorii (folosește Standard Shader sau URP/Lit)

### Controllers nu apar:
- Verifică XR Rig setup
- Verifică că XR Plugin Management este configurat corect
- Testează cu XR Device Simulator în Editor

---

## Note Importante

- **Performance**: Acest prototype folosește schimbări de culoare în runtime - pentru producție, consideră material swapping
- **Platforms**: Testat pentru OpenXR (Quest, PCVR)
- **Unity Version**: Requires Unity 2021.3+ cu XR Interaction Toolkit 2.0+

---

Succes cu prototype-ul! 🎮🥽

