# Unity Minecraft-Style Inventory System Setup Instructions

**Beginner-Friendly Step-by-Step Guide**

This guide will walk you through setting up a simple 3-slot Minecraft-style inventory system with very detailed instructions.

## Features
- 3 inventory slots
- Items automatically go to first open slot
- Number keys 1, 2, 3 to switch between items
- Hand UI shows current item
- Press G to drop current item
- "Inventory is Full" message when trying to pick up with full inventory

---

## Step 1: Set Up Inventory UI Elements

### Part A: Create Slot 1 Icon

1. **In the Unity Editor, look at the Hierarchy window** (usually on the left side)
2. **Find your Canvas** - If you don't have one:
   - Right-click in the Hierarchy window (empty space)
   - Click **UI** → **Canvas**
   - A Canvas and EventSystem will appear
3. **Right-click on the Canvas** in the Hierarchy
4. Click **UI** → **Image**
5. A new Image will appear under Canvas
6. **Click on this Image** to select it
7. **Look at the Inspector window** (usually on the right side)
8. **At the top of the Inspector**, you'll see the name "Image" - **click on it and type "Slot1Icon"** (or just rename it in the Hierarchy)
9. **In the Inspector, find the Image component:**
   - Find the checkbox that says **"Raycast Target"** - make sure it's **checked**
   - Find **"Image Type"** dropdown - set it to **"Simple"**
   - Find the **"Color"** field - click the color box and set it to white (or any color you want)
   - Find the checkbox that says **"Enabled"** - **UNCHECK this** (we'll enable it when there's an item)
10. **In the Inspector, find the Rect Transform component:**
    - Set **Width** to 80 (or your preferred size)
    - Set **Height** to 80 (or your preferred size)
    - Position it where you want slot 1 to appear (typically bottom-left area of screen)

### Part B: Create Slot 2 Icon

1. **Right-click on Slot1Icon** in the Hierarchy
2. Click **Duplicate** (or press Ctrl+D)
3. A new "Slot1Icon" will appear - **click on it** to select it
4. **Rename it to "Slot2Icon"** (click the name in Hierarchy and type)
5. **In the Inspector**, make sure:
   - **"Enabled"** is **unchecked**
   - **Image Type** is **"Simple"**
6. **Move it** to the right of Slot1Icon (in the Scene view or by adjusting Rect Transform position)

### Part C: Create Slot 3 Icon

1. **Right-click on Slot2Icon** in the Hierarchy
2. Click **Duplicate** (or press Ctrl+D)
3. **Rename it to "Slot3Icon"**
4. **In the Inspector**, make sure:
   - **"Enabled"** is **unchecked**
   - **Image Type** is **"Simple"**
5. **Move it** to the right of Slot2Icon

### Part D: Create Hand Icon (Shows Current Item)

1. **Right-click on the Canvas** in the Hierarchy
2. Click **UI** → **Image**
3. **Rename it to "HandIcon"**
4. **In the Inspector:**
   - Set **Image Type** to **"Simple"**
   - **UNCHECK "Enabled"** (we'll enable it when holding an item)
   - Set **Width** to 100
   - Set **Height** to 100
5. **Position it** where you want the current item to show (typically center-bottom of screen, above the slots)

### Part E: Create "Inventory Full" Message

1. **Right-click on the Canvas** in the Hierarchy
2. Click **UI** → **Text - TextMeshPro** (if it asks to import TMP Essentials, click "Import")
3. **Rename it to "InventoryFullMessage"**
4. **In the Inspector, find the TextMeshPro component:**
   - **Text** field: Type "Inventory is Full" (or leave empty, code will set it)
   - **Font Size**: Set to 36 (or larger if you want)
   - **Alignment**: Click the center alignment button (middle row, center button)
   - **Color**: Click the color box and choose **red** or **yellow** for visibility
5. **In the Inspector, find the Rect Transform:**
   - Set **Width** to 400
   - Set **Height** to 100
   - Position it in the **center of the screen**
6. **At the top of the Inspector**, find the checkbox next to the GameObject name - **UNCHECK it** to make it inactive (we'll show it when needed)

---

## Step 2: Set Up InventoryManager Script

### Part A: Create the InventoryManager GameObject

1. **In the Hierarchy window**, right-click in empty space
2. Click **Create Empty**
3. A new "GameObject" will appear
4. **Click on it** to select it
5. **In the Inspector**, at the top where it says "GameObject", **rename it to "InventoryManager"**

### Part B: Add the InventoryManager Component

1. **With InventoryManager selected** in the Hierarchy
2. **Look at the Inspector window**
3. **At the bottom of the Inspector**, click the **"Add Component"** button
4. **In the search box**, type "InventoryManager"
5. **Click on "InventoryManager"** in the list
6. The InventoryManager component will appear in the Inspector

### Part C: Assign UI Elements to InventoryManager

1. **Make sure InventoryManager is selected** in the Hierarchy
2. **In the Inspector**, you'll see the InventoryManager component with several fields
3. **Find "Slot 1 Icon" field:**
   - **Click and drag** "Slot1Icon" from the Hierarchy into this field
4. **Find "Slot 2 Icon" field:**
   - **Click and drag** "Slot2Icon" from the Hierarchy into this field
5. **Find "Slot 3 Icon" field:**
   - **Click and drag** "Slot3Icon" from the Hierarchy into this field
6. **Find "Hand Icon" field:**
   - **Click and drag** "HandIcon" from the Hierarchy into this field
7. **Find "Inventory Full Message" field:**
   - **Click and drag** "InventoryFullMessage" from the Hierarchy into this field
8. **Find "Message Display Time" field:**
   - **Click in the number field** and type **2** (this is how many seconds the message shows)
9. **Find "Input Actions" field:**
   - **Look at the Project window** (usually at the bottom)
   - **Navigate to**: Assets → Scripts
   - **Find "PlayerControls"** (it should be an Input Actions asset)
   - **Click and drag** "PlayerControls" into the "Input Actions" field

---

## Step 3: Set Up PlayerController

### Part A: Find Your Player GameObject

1. **In the Hierarchy**, look for your player GameObject (might be named "Player", "PlayerController", or similar)
2. **If you can't find it**, check if there's a prefab - look in the Project window under Assets → Prefabs
3. **Click on your Player GameObject** in the Hierarchy to select it

### Part B: Assign InventoryManager to PlayerController

1. **With your Player selected**, look at the **Inspector window**
2. **Find the "PlayerController" component** (scroll down if needed)
3. **Find the "Inventory Manager" field** in the PlayerController component
4. **Click and drag** "InventoryManager" from the Hierarchy into this field

---

## Step 4: Create a Test Item to Pick Up

### Part A: Create the Item GameObject

1. **In the Hierarchy**, right-click in empty space
2. Click **3D Object** → **Cube** (or Sphere, or use your own model)
3. **Rename it** to something like "TestItem" or "Apple"
4. **Click on it** to select it
5. **In the Inspector**, find the **Transform component**:
   - Set **Position** to something like X: 0, Y: 1, Z: 5 (in front of where your player starts)
   - Set **Scale** to X: 0.5, Y: 0.5, Z: 0.5 (to make it smaller)

### Part B: Add Collider (if not already there)

1. **With your item selected**, look at the **Inspector**
2. **Check if there's a "Box Collider" component** (or Sphere Collider, etc.)
3. **If there's no collider:**
   - Click **"Add Component"** at the bottom of the Inspector
   - Type "Box Collider" in the search
   - Click on "Box Collider"
4. **Make sure "Is Trigger" is UNCHECKED** (we need a solid collider for raycasting)

### Part C: Set the Item's Layer

1. **With your item selected**, look at the **Inspector**
2. **At the top**, find the **"Layer"** dropdown (next to "Tag")
3. **Click the dropdown** and select **"Interactable"**
4. **If "Interactable" layer doesn't exist:**
   - Click **"Add Layer..."**
   - Find an empty slot (like "Layer 8" or "Layer 9")
   - **Click in the name field** and type "Interactable"
   - **Close the window**
   - **Go back to your item** and set the Layer to "Interactable"

### Part D: Add InteractableItem Component

1. **With your item selected**, click **"Add Component"** at the bottom of the Inspector
2. **Type "InteractableItem"** in the search box
3. **Click on "InteractableItem"** in the list
4. **In the Inspector**, you'll see the InteractableItem component:
   - **Item Name**: Type a name like "Apple" or "Test Item"
   - **Icon**: 
     - **Look at the Project window**
     - **Find or create a sprite** (you can use any image - right-click in Project → Import New Asset → select an image file)
     - **Click and drag** the sprite into the "Icon" field
   - **Item Data**: Leave this empty (it's optional)

### Part E: Set Up PlayerController for Item Interaction

1. **Select your Player GameObject** in the Hierarchy
2. **In the Inspector**, find the **PlayerController component**
3. **Find "Interactable Layer" field:**
   - **Click the dropdown** (it might say "Nothing" or "Everything")
   - **Click on "Interactable"** layer
4. **Find "Interact Range" field:**
   - **Click in the number field** and type **3** (this is how far away you can pick up items)

---

## Step 5: Test the System

### Part A: Play the Scene

1. **Click the Play button** at the top center of Unity (the triangle button)
2. **Your game should start playing**

### Part B: Test Item Pickup

1. **Look at your test item** in the game view
2. **Move your mouse** so you're looking at the item
3. **Press E** on your keyboard
4. **The item should disappear** and appear in slot 1
5. **The hand icon should show** the item you picked up

### Part C: Test Slot Switching

1. **Pick up another item** (create another test item if needed)
2. **Press the number 1 key** - should switch to slot 1
3. **Press the number 2 key** - should switch to slot 2
4. **Press the number 3 key** - should switch to slot 3
5. **The hand icon should change** to show the item in the selected slot

### Part D: Test Dropping

1. **Make sure you have an item in your current slot**
2. **Press G** on your keyboard
3. **The item should appear** in front of you in the world
4. **The slot should become empty**

### Part E: Test Inventory Full Message

1. **Pick up 3 items** to fill all slots
2. **Try to pick up a 4th item**
3. **You should see "Inventory is Full"** message appear in the center of the screen
4. **The message should disappear** after 2 seconds

---

## Troubleshooting

### Items not picking up:
- **Check the item's Layer**: Make sure it's set to "Interactable"
- **Check PlayerController**: Make sure "Interactable Layer" is set to "Interactable"
- **Check distance**: Make sure you're close enough (within 3 units)
- **Check the item has InteractableItem component**: Select the item and check the Inspector

### Number keys not switching:
- **Make sure InventoryManager is in the scene**: Check the Hierarchy
- **Check for errors**: Look at the Console window (Window → General → Console)
- **Make sure you have items in slots**: Empty slots won't show anything

### Hand icon not showing:
- **Check HandIcon is assigned**: Select InventoryManager and check the Inspector
- **Check item has an icon**: Select your item and make sure the Icon field has a sprite assigned
- **Check HandIcon Image is enabled**: When you have an item, the Image component should be enabled automatically

### Drop not working:
- **Check you have an item selected**: Make sure a slot with an item is currently selected
- **Check PlayerController has playerCam**: Select Player and make sure "Player Cam" is assigned
- **Check Console for errors**: Window → General → Console

### "Inventory is Full" message not showing:
- **Check InventoryFullMessage is assigned**: Select InventoryManager and check the Inspector
- **Check the GameObject is set up**: Select InventoryFullMessage and make sure it has a TextMeshPro component
- **Check it's inactive initially**: The checkbox at the top of Inspector should be unchecked when no message is showing

---

## Quick Reference

**Controls:**
- **E** - Pick up item
- **1, 2, 3** - Switch between slots
- **G** - Drop current item

**What goes where:**
- **Slot1Icon, Slot2Icon, Slot3Icon** → InventoryManager "Slot X Icon" fields
- **HandIcon** → InventoryManager "Hand Icon" field
- **InventoryFullMessage** → InventoryManager "Inventory Full Message" field
- **InventoryManager** → PlayerController "Inventory Manager" field
- **PlayerControls** (Input Actions) → InventoryManager "Input Actions" field

---

## Notes

- The inventory system is simple - no drag and drop needed
- Items automatically go to the first empty slot
- The hand always shows what's in your currently selected slot
- If a slot is empty, the hand will be empty too
- You can have up to 3 items at once
