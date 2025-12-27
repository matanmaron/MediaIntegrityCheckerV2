# MediaIntegrityCheckerV2

**MediaIntegrityCheckerV2** is a lightweight Dockerized application for maintaining the integrity of media files (photos, videos, etc.) in a single directory. It continuously monitors files by computing checksums, logging changes, and highlighting potential corruption or locked files. The app is designed for home servers or personal NAS-like setups without the complexity of full NAS software.

---

## Features

- **Checksum-based file verification:**  
  Computes SHA-256 hashes for all files in the target directory. Detects new files, changes (potential corruption), and locked/skipped files.

- **Web UI:**  
  Simple, single-page interface accessible via browser:
    - Start/Stop button for scanning
    - Real-time log updates
    - Copy log to clipboard

- **Color-coded logs in web UI:**
    - `[NEW]` — green (new files detected)
    - `[LOCK]` — yellow (files locked or skipped)
    - `[BAD]` — red (files changed or corrupted)
    - `[OK]` — white (unchanged files, hidden in the UI but stored in log file)

- **Persistent logging:**
    - Each scan creates a timestamped log file in `logs/`.
    - Full history of scans kept, including OK files, for audit purposes.

- **Checksum storage:**
    - Database of file paths and hashes stored in `data/checksums.db`.
    - Detects changes between runs to identify corruption.

- **Dockerized deployment:**
    - Can be mounted to any directory using Docker volumes.
    - Paths for scanning, data, and logs are configurable via volume mounts.

---

## Setup & Installation

1. **Clone repository:**
    ```bash
    git clone https://github.com/yourusername/MediaIntegrityCheckerV2.git
    cd MediaIntegrityCheckerV2
    ```

2. **Build Docker image:**
    ```bash
    docker build -t media-checker .
    ```

3. **Run Docker container:**
    ```bash
    docker run -d \
      --name MediaIntegrityCheckerV2 \
      -p 5000:5000 \
      -v /path/to/media:/scan \
      -v /path/to/data:/app/data \
      -v /path/to/logs:/app/logs \
      --restart unless-stopped \
      media-checker
    ```
   Replace `/path/to/media`, `/path/to/data`, and `/path/to/logs` with your actual directories. The `-v` flags define the paths used by the application.

4. **Access Web UI:**  
   Open your browser at `http://<server-ip>:5000/`.

---

## Usage

- Click **START** to begin scanning the target directory.
- The **log box** will update in real-time showing `[NEW]`, `[BAD]`, `[LOCK]` entries in color.
- Summary line displays colored counts of new, skipped, and bad files.
- Click **STOP** to stop the scan.
- Click **COPY LOG** to copy the current log content to the clipboard.

---

## Limitations

- Designed for **light home use** — single or few users. Not optimized for multi-user NAS setups.
- **Corruption detection is hash-based only**. Cannot repair files — only detect changes.
- Large file sets may take time to scan; performance depends on hardware.
- Logs are maintained **per scan** in plain text; no database-level transaction support.
- Only scans the directory provided via Docker volume mount. Does not traverse multiple disconnected storage locations automatically.
- Unicode filenames (e.g., Hebrew) are supported, but symbolic links or shortcuts are ignored.

---

## Goals

- Provide a simple, Dockerized integrity checker for media files.
- Detect new, modified, or locked files before propagating them to backups or other systems.
- Maintain a visual, real-time log with color-coding for quick understanding of file status.
- Allow eventual expansion to multiple scan directories or scheduled scans via Docker scheduling.

---

## File Structure

MediaIntegrityCheckerV2/
├─ data/ # Checksum database
├─ logs/ # Scan logs
├─ wwwroot/ # Static web UI files (HTML, JS, CSS)
├─ Program.cs # Main .NET 8 web API and endpoints
├─ ScanService.cs # Checksum scanning logic
├─ Dockerfile # Docker build instructions
└─ README.md


---

## Technical Notes

- Written in **.NET 8 (C#)** with minimal Web API.
- Uses **SHA-256** checksums to verify file integrity.
- Database is a simple plain text `checksums.db` in `data/`.
- Web UI uses plain HTML/JS with color-coded `<span>`s for log entries.
- All `[OK]` entries are recorded in log file but hidden in the UI to reduce noise.

---

## Contributing

- Fork the repo and submit pull requests.
- Suggestions for additional features (e.g., scheduled scans, multi-directory support) are welcome.

---

## License

MIT License
