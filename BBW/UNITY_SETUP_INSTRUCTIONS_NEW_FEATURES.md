# Unity Setup Instructions for New Inventory Features

**Detailed Step-by-Step Guide**

This guide will walk you through setting up all the new features: slot backgrounds, selected slot highlighting, 3D items in hand, crouch control, and fixing item visibility issues.

---

## Step 1: Set Up Slot Backgrounds (Always Visible White Boxes)

### Part A: Create Background for Slot 1

1. **In the Hierarchy window** (left side of Unity), find your **Slot1Icon** GameObject
2. **Right-click on Slot1Icon**
3. Click **UI** → **Image**
4. A new Image will appear as a child of Slot1Icon
5. **Click on this new Image** to select it
6. **Rename it** to "Slot1Background" (click the name in Hierarchy and type)
7. **In the Inspector window** (right side):
   - **Find the Rect Transform component:**
     - Click the **anchor presets** button (square icon at top-left of Rect Transform)
     - **Hold Shift + Alt** and click **"Stretch Both"** (bottom-right option)
     - This makes the background fill the entire slot
   - **Find the Image component:**
     - **Color**: Click the color box and set it to **white** (or your preferred background color)
     - **Image Type**: Set to **"Simple"**
     - **Make sure "Enabled" is CHECKED** (this should always be visible)
     - **Raycast Target**: You can uncheck this (not needed for background)

### Part B: Create Background for Slot 2

1. **Right-click on Slot2Icon** in Hierarchy
2. Click **UI** → **Image**
3. **Rename it** to "Slot2Background"
4. **In the Inspector:**
   - **Rect Transform**: Click anchor presets → Hold Shift + Alt → Click "Stretch Both"
   - **Image component**: Set color to white, Image Type to "Simple", Enabled checked

### Part C: Create Background for Slot 3

1. **Right-click on Slot3Icon** in Hierarchy
2. Click **UI** → **Image**
3. **Rename it** to "Slot3Background"
4. **In the Inspector:**
   - **Rect Transform**: Click anchor presets → Hold Shift + Alt → Click "Stretch Both"
   - **Image component**: Set color to white, Image Type to "Simple", Enabled checked

### Part D: Reorder Children (Background Behind Icon)

1. **For each slot** (Slot1Icon, Slot2Icon, Slot3Icon):
   - **In the Hierarchy**, the background should be **above** the icon in the list
   - If it's not, **click and drag** the background **above** the icon
   - Unity renders children from top to bottom, so background should be first

---

## Step 2: Assign Slot Backgrounds to InventoryManager

1. **In the Hierarchy**, find and **click on "InventoryManager"** GameObject
2. **In the Inspector**, find the **InventoryManager component**
3. **Scroll down** to find the **"Inventory Slots"** section
4. **Find "Slot 1 Background" field:**
   - **Click and drag** "Slot1Background" from Hierarchy into this field
5. **Find "Slot 2 Background" field:**
   - **Click and drag** "Slot2Background" from Hierarchy into this field
6. **Find "Slot 3 Background" field:**
   - **Click and drag** "Slot3Background" from Hierarchy into this field
7. **Find "Selected Slot Color" field:**
   - **Click the color box** and choose a highlight color (default is yellow)
   - This is the color the selected slot will turn
8. **Find "Normal Slot Color" field:**
   - **Click the color box** and choose white (or your background color)
   - This is the normal slot color

---

## Step 3: Set Up Hand Hold Point (For 3D Items in Hand)

### Part A: Find Your Player Camera

1. **In the Hierarchy**, find your **Player GameObject**
2. **Expand it** (click the arrow next to it) to see children
3. **Look for the camera** - it might be named:
   - "Main Camera"
   - "Camera"
   - "PlayerCamera"
   - Or it might be a child of the player
4. **Click on the camera** to select it
5. **Remember which one it is** - you'll need it in the next step

### Part B: Create Hand Hold Point

1. **Right-click on your player camera** in the Hierarchy
2. Click **Create Empty**
3. A new "GameObject" will appear as a child of the camera
4. **Click on it** to select it
5. **Rename it** to "HandHoldPoint"
6. **In the Inspector**, find the **Transform component:**
   - **Position X**: Type **0.3** (slightly to the right)
   - **Position Y**: Type **-0.2** (slightly down)
   - **Position Z**: Type **0.5** (in front of camera)
   - **Rotation X**: Type **0**
   - **Rotation Y**: Type **0**
   - **Rotation Z**: Type **0**
   - **Scale**: Leave at **1, 1, 1**
7. **Adjust these values** as needed to position items where you want them in hand

### Part C: Assign Hand Hold Point to InventoryManager

1. **Click on "InventoryManager"** in Hierarchy
2. **In the Inspector**, find the **InventoryManager component**
3. **Find "Hand Hold Point" field** (in the "Hand UI" section)
4. **Click and drag** "HandHoldPoint" from Hierarchy into this field

---

## Step 4: Fix Item Visibility (Camera Culling Mask)

### Part A: Check Camera Culling Mask

1. **In the Hierarchy**, find and **click on your Main Camera** (or player camera)
2. **In the Inspector**, find the **Camera component**
3. **Find "Culling Mask" dropdown** (it shows which layers the camera can see)
4. **Click the dropdown** to see all layers
5. **Make sure "Interactable" layer has a CHECKMARK** next to it
   - If it's unchecked, **click on "Interactable"** to check it
6. **If "Interactable" layer doesn't exist in the list:**
   - See Step 5 to create it first

### Part B: Verify Items Are Visible

1. **Click Play** button (top center)
2. **Look at your items** in the game view
3. **You should see the full item**, not just the shadow
4. **If you still only see shadows:**
   - Make sure items are on "Interactable" layer (see Step 5)
   - Check that camera Culling Mask includes "Interactable"
   - Make sure items have Mesh Renderer components enabled

---

## Step 5: Fix Item Layers (Make Sure Items Are on Interactable Layer)

### Part A: Create Interactable Layer (If It Doesn't Exist)

1. **At the top of Unity**, click **Edit** → **Project Settings**
2. **In the left panel**, click **"Tags and Layers"**
3. **In the right panel**, find **"Layers"** section
4. **Find an empty layer slot** (like "Layer 8" or "Layer 9")
5. **Click in the name field** next to the empty slot
6. **Type "Interactable"**
7. **Close the Project Settings window**

### Part B: Set Items to Interactable Layer

1. **In the Hierarchy**, find one of your item GameObjects
2. **Click on it** to select it
3. **At the top of the Inspector**, find the **"Layer" dropdown** (next to "Tag")
4. **Click the dropdown**
5. **Select "Interactable"** from the list
6. **A popup will appear** asking "Do you want to set layer for all child objects?"
   - **Click "Yes, change children"** (this applies to all child objects too)
7. **Repeat for all pickup items** in your scene

### Part C: Verify Items Are NOT on UI Layer

1. **Select each item** and check the Layer dropdown
2. **Make sure it says "Interactable"**, NOT "UI"
3. **If an item is on "UI" layer:**
   - Change it to "Interactable" layer (follow Step 5B)
   - Items on UI layer can't be interacted with using physics raycasts

---

## Step 6: Set Up Pickup Prompt (If Not Already Done)

### Part A: Create Pickup Prompt Text

1. **In the Hierarchy**, find your **Canvas**
2. **Right-click on Canvas**
3. Click **UI** → **Text - TextMeshPro** (if it asks to import TMP Essentials, click "Import")
4. **Rename it** to "PickupPrompt"
5. **In the Inspector**, find the **TextMeshPro component:**
   - **Text**: Type "Press E to pick up Item" (or leave empty, code will set it)
   - **Font Size**: Set to **24** (or larger for visibility)
   - **Alignment**: Click the **center alignment** button (middle row, center button)
   - **Color**: Click color box and choose **white** or **yellow** for visibility
6. **In the Inspector**, find the **Rect Transform:**
   - **Anchor Presets**: Click the square icon → Hold **Alt** → Click **"Middle Center"**
   - **Position Y**: Type **-200** (this positions it below center of screen)
   - **Width**: Type **400**
   - **Height**: Type **50**

### Part B: Assign Pickup Prompt to PlayerController

1. **In the Hierarchy**, find and **click on your Player GameObject**
2. **In the Inspector**, find the **PlayerController component**
3. **Find "Pickup Prompt" field** (in the "Prompt UI" section)
4. **Click and drag** "PickupPrompt" from Hierarchy into this field
5. **Make sure the field is not empty** - it should show "PickupPrompt (TextMeshPro - Text (UI))"

---

## Step 7: Configure Crouch Settings (Optional - Adjust If Needed)

1. **In the Hierarchy**, find and **click on your Player GameObject**
2. **In the Inspector**, find the **PlayerController component**
3. **Find "Movement" section:**
   - **Crouch Speed**: Type **5** (or adjust to your preference - this is how fast you move while crouching)
   - **Crouch Height**: Type **0.5** (or adjust - this is how tall the player is when crouching)
   - **Normal Height**: Type **2** (or adjust - this is the normal player height)
4. **These values depend on your player's collider setup**
   - If your player uses a Capsule Collider, these should work
   - If you use a different collider, you may need to adjust the code

---

## Step 8: Test Everything

### Part A: Test Slot Backgrounds

1. **Click Play** button
2. **Look at your inventory slots** at the bottom of the screen
3. **You should see white boxes** (backgrounds) for all 3 slots
4. **The backgrounds should always be visible**, even when slots are empty

### Part B: Test Selected Slot Highlighting

1. **While playing**, press **number 1** key
2. **Slot 1 background should change color** (to yellow or your selected color)
3. **Press number 2** key
4. **Slot 2 should highlight**, slot 1 should return to normal
5. **Press number 3** key
6. **Slot 3 should highlight**, slot 2 should return to normal

### Part C: Test 3D Item in Hand

1. **Pick up an item** (press E while looking at an item)
2. **Look at your hand/camera view**
3. **You should see the 3D item model** appear in front of you
4. **Press number 2** to switch to slot 2
5. **If slot 2 has an item**, that item should appear in hand
6. **If slot 2 is empty**, hand should be empty

### Part D: Test Crouch

1. **While playing**, **hold down Ctrl** key
2. **Your player should crouch down** (get shorter)
3. **You should move slower** while crouching
4. **Release Ctrl**
5. **Player should stand back up** and move at normal speed

### Part E: Test Item Visibility

1. **Look at items in the scene** (before picking them up)
2. **You should see the full 3D model**, not just shadows
3. **If you only see shadows:**
   - Check camera Culling Mask (Step 4)
   - Check item layers (Step 5)
   - Make sure items have Mesh Renderer components enabled

### Part F: Test Pickup Prompt

1. **Look at an item** in the game
2. **You should see "Press E to pick up [Item Name]"** text appear
3. **Move away from the item**
4. **The prompt should disappear**
5. **If prompt doesn't show:**
   - Check that PickupPrompt is assigned in PlayerController (Step 6)
   - Check that PickupPrompt GameObject is active (checkbox at top of Inspector)
   - Check that the text color is visible (not black on black background)

---

## Troubleshooting

### Slot backgrounds not showing:
- **Check that backgrounds are children of slot icons** (Slot1Background should be child of Slot1Icon)
- **Check that background Images are enabled** (Enabled checkbox should be checked)
- **Check Rect Transform anchors** - backgrounds should stretch to fill parent
- **Check background is above icon in Hierarchy** (renders first/behind)

### Selected slot not highlighting:
- **Check that slot backgrounds are assigned** in InventoryManager
- **Check Selected Slot Color** is set (not transparent/black)
- **Check Normal Slot Color** is different from selected color
- **Try clicking the three dots (⋮) menu** on InventoryManager component → **Reset**

### 3D item not appearing in hand:
- **Check Hand Hold Point is assigned** in InventoryManager
- **Check Hand Hold Point position** - it might be too far or behind camera
- **Check that items have Mesh Renderer** components
- **Check Hand Hold Point is child of camera** (should move with camera)
- **Try adjusting Hand Hold Point position** values in Transform

### Crouch not working:
- **Check that player has Capsule Collider** component
- **Check Crouch Height and Normal Height** values are reasonable
- **Try pressing Left Ctrl or Right Ctrl** (both should work)
- **Check Console for errors** (Window → General → Console)

### Items still only showing shadows:
- **Check camera Culling Mask** includes "Interactable" layer
- **Check items are on "Interactable" layer**, not "UI" or "Default"
- **Check items have Mesh Renderer** components and they're enabled
- **Check items are not too far from camera** (might be culled by distance)
- **Try selecting the camera** → Inspector → Camera → **Near Clipping Plane** - set to **0.01** (very close)

### Pickup prompt not showing:
- **Check PickupPrompt is assigned** in PlayerController
- **Check PickupPrompt GameObject is active** (checkbox at top of Inspector)
- **Check PickupPrompt is on Canvas** (should be child of Canvas)
- **Check text color is visible** (white/yellow, not black)
- **Check PlayerController has Interactable Layer set** correctly
- **Check items have InteractableItem component**

---

## Quick Checklist

Before testing, make sure:
- [ ] Slot backgrounds created and assigned to InventoryManager
- [ ] Hand Hold Point created and assigned to InventoryManager
- [ ] Camera Culling Mask includes "Interactable" layer
- [ ] All items are on "Interactable" layer (not UI layer)
- [ ] PickupPrompt is created and assigned to PlayerController
- [ ] Player has Capsule Collider (for crouch to work)
- [ ] All fields in InventoryManager are assigned (no empty fields)

---

## Notes

- **Slot backgrounds** are separate from slot icons - backgrounds always show, icons only show when items are present
- **Hand Hold Point** should be a child of your camera so it moves with your view
- **Items must be on Interactable layer** for physics raycasting to work
- **Camera must see Interactable layer** in its Culling Mask for items to be visible
- **Crouch uses Capsule Collider** - if you use a different collider type, you may need to adjust the code
