# 🎮 Multiplayer Puzzle Game

A networked multiplayer puzzle game built with Unity and PurrNet, featuring synchronized mechanics, interactive puzzles, and peer-to-peer networking.

---

## ✨ Overview

This project demonstrates a scalable multiplayer game architecture with a focus on synchronization, modular systems, and performance.

### Key Highlights
- 🔗 Real-time multiplayer synchronization across clients  
- 🧩 Interactive puzzle systems (clock-based mechanics)  
- 🧠 State-driven gameplay flow (waiting → spawning → gameplay)  
- 🔌 Modular interaction system using `IInteractable`  
- 🔊 Network-synced audio and UI feedback  
- ⚡ Optimized for performance and scalability  

---

## 🚀 Features

### 🌐 Multiplayer Framework
- Player spawning, movement, and ownership handling  
- Peer-to-peer networking via Steam Relay  
- Smooth synchronization across clients  

### 🧩 Puzzle System
- Clock-based puzzles with cross-client synchronization  
- Modular puzzle architecture for easy expansion  

### 🎮 Player Controller
- Movement (walk, sprint, jump)  
- Camera look  
- Network-optimized input handling  

### 🖥️ UI System
- Interaction prompts  
- Puzzle status display  
- Modular view management via `GameViewManager`  

### 🔊 Audio System
- Synchronized tick sounds  
- Feedback for interactions and puzzle events  

### ⚡ Optimization
- Object pooling  
- Efficient Update loops  
- Network ownership transfer optimization  

---

## 🛠️ Tech Stack

- Unity 2021.3+  
- PurrNet (Multiplayer Networking)  
- Steam Relay (Peer-to-peer connection)  

---

## 🧱 Architecture Highlights

- Component-based design (Unity best practice)  
- Interface-driven interactions (`IInteractable`)  
- State machine for game flow  
- Decoupled systems for scalability  

---

## 📌 Notes

- Designed as a portfolio project to demonstrate multiplayer architecture  
- Easily extendable with new puzzle types and mechanics  
