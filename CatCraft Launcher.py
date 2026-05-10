import sys
import os
import json
import urllib.request
from functools import partial

from PyQt5.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QStackedWidget, QLabel, QFrame, QScrollArea,
    QDialog, QListWidget, QListWidgetItem, QDialogButtonBox,
    QMessageBox, QCheckBox, QLineEdit, QFormLayout,
    QFileDialog, QStyleFactory, QProgressBar, QTextEdit,
    QSizePolicy, QSpacerItem
)
from PyQt5.QtCore import Qt, QThread, pyqtSignal, QTimer, QPoint, QPropertyAnimation, QEasingCurve
from PyQt5.QtGui import QFont, QTextCursor

# ---------- 辅助函数（必须在调用前定义）----------
def get_current_dir():
    if getattr(sys, 'frozen', False):
        return os.path.dirname(sys.executable)
    else:
        return os.getcwd()

# ---------- 版本号 ----------
APP_VERSION = "Dev 1.0"

# ---------- URL配置 ----------
GAME_EXE_URL = "https://m1954420.772988.xyz/ccl/catcraft.exe"
CREATOR_JSON_URL = "https://m1954420.772988.xyz/ccl/creator.json"

CURRENT_DIR = get_current_dir()
CONFIG_DIR = os.path.join(CURRENT_DIR, ".ccl")
CONFIG_FILE = os.path.join(CONFIG_DIR, "option.json")

DEFAULT_CONFIG = {
    "game_root_dir": CURRENT_DIR,
    "tips_at_close": True
}

# ---------- 配置管理 ----------
class ConfigManager:
    def __init__(self):
        self.config = {}
        self.load()

    def load(self):
        if os.path.exists(CONFIG_FILE):
            try:
                with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
                    self.config = json.load(f)
            except:
                self.config = DEFAULT_CONFIG.copy()
        else:
            self.config = DEFAULT_CONFIG.copy()
            self.save()

    def save(self):
        os.makedirs(CONFIG_DIR, exist_ok=True)
        with open(CONFIG_FILE, 'w', encoding='utf-8') as f:
            json.dump(self.config, f, indent=4, ensure_ascii=False)

    def get(self, key, default=None):
        return self.config.get(key, default)

    def set(self, key, value):
        self.config[key] = value
        self.save()

    def get_game_exe_path(self):
        root = self.get("game_root_dir", CURRENT_DIR)
        return os.path.join(root, ".catcraft", "version", "catcraft.exe")

    def get_creator_json_path(self):
        root = self.get("game_root_dir", CURRENT_DIR)
        return os.path.join(root, ".ccl", "creator.json")

# ---------- 下载线程（用于下载creator.json）----------
class SimpleDownloadThread(QThread):
    finished = pyqtSignal(bool, str)

    def __init__(self, url, save_path):
        super().__init__()
        self.url = url
        self.save_path = save_path

    def run(self):
        try:
            os.makedirs(os.path.dirname(self.save_path), exist_ok=True)
            req = urllib.request.Request(self.url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=30) as response:
                with open(self.save_path, 'wb') as f:
                    f.write(response.read())
            self.finished.emit(True, self.save_path)
        except Exception as e:
            self.finished.emit(False, str(e))

# ---------- 下载线程（用于游戏主程序）----------
class DownloadThread(QThread):
    progress = pyqtSignal(int)
    finished = pyqtSignal(bool, str)

    def __init__(self, url, save_path):
        super().__init__()
        self.url = url
        self.save_path = save_path

    def run(self):
        try:
            os.makedirs(os.path.dirname(self.save_path), exist_ok=True)
            req = urllib.request.Request(self.url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=30) as response:
                total = int(response.headers.get('Content-Length', 0))
                downloaded = 0
                with open(self.save_path, 'wb') as out_file:
                    chunk_size = 8192
                    while True:
                        chunk = response.read(chunk_size)
                        if not chunk:
                            break
                        out_file.write(chunk)
                        downloaded += len(chunk)
                        if total:
                            percent = int(downloaded * 100 / total)
                            self.progress.emit(percent)
            self.finished.emit(True, self.save_path)
        except Exception as e:
            self.finished.emit(False, str(e))


# ---------- 贡献名单对话框（滚动字幕效果）----------

class CreditsDialog(QDialog):
    def __init__(self, data, parent=None):
        super().__init__(parent)
        self.setWindowTitle(f"贡献名单 - {APP_VERSION}")
        self.setModal(True)
        self.setFixedSize(500, 400)
        self.setStyleSheet("background-color: #f57c00;")

        # 滚动区域（无滚动条）
        self.scroll_area = QScrollArea(self)
        self.scroll_area.setWidgetResizable(False)
        self.scroll_area.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.scroll_area.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.scroll_area.setStyleSheet("border: none; background-color: #f57c00;")

        # 内容容器
        self.content_widget = QWidget()
        self.content_widget.setStyleSheet("background-color: #f57c00;")
        layout = QVBoxLayout(self.content_widget)
        layout.setAlignment(Qt.AlignTop)
        layout.setSpacing(15)
        layout.setContentsMargins(20, 20, 20, 20)

        # 构建内容
        developers = data.get("开发人员", [])
        if developers:
            title = QLabel("开发人员")
            title_font = QFont()
            title_font.setPointSize(18)
            title_font.setBold(True)
            title.setFont(title_font)
            title.setStyleSheet("color: black;")
            title.setAlignment(Qt.AlignCenter)
            layout.addWidget(title)
            for dev in developers:
                name = QLabel(dev)
                name_font = QFont()
                name_font.setPointSize(12)
                name.setFont(name_font)
                name.setStyleSheet("color: white;")
                name.setAlignment(Qt.AlignCenter)
                layout.addWidget(name)

        thanks = data.get("致谢人员", [])
        if thanks:
            title = QLabel("致谢人员")
            title_font = QFont()
            title_font.setPointSize(18)
            title_font.setBold(True)
            title.setFont(title_font)
            title.setStyleSheet("color: black;")
            title.setAlignment(Qt.AlignCenter)
            layout.addWidget(title)
            for person in thanks:
                name = QLabel(person)
                name_font = QFont()
                name_font.setPointSize(12)
                name.setFont(name_font)
                name.setStyleSheet("color: white;")
                name.setAlignment(Qt.AlignCenter)
                layout.addWidget(name)

        # 底部留白，保证最后一行完全滚出视口
        layout.addSpacing(400)

        self.scroll_area.setWidget(self.content_widget)

        # 主布局
        main_layout = QVBoxLayout(self)
        main_layout.addWidget(self.scroll_area)
        self.setLayout(main_layout)

        self.scroll_started = False

    def showEvent(self, event):
        super().showEvent(event)
        if not self.scroll_started:
            QTimer.singleShot(100, self.start_scroll)
            self.scroll_started = True

    def start_scroll(self):
        viewport = self.scroll_area.viewport()
        viewport_width = viewport.width()
        viewport_height = viewport.height()

        # 强制内容宽度等于视口宽度，内部 QLabel 自然水平居中
        self.content_widget.setFixedWidth(viewport_width)

        content_height = self.content_widget.sizeHint().height()
        if content_height <= viewport_height:
            self.accept()
            return

        # 初始位置：内容完全在视口下方（顶部刚好在视口底部边界）
        start_y = viewport_height
        # 结束位置：内容完全移出视口上方（底部刚好在视口顶部边界）
        end_y = -content_height
        distance = start_y - end_y  # 等于 viewport_height + content_height

        # 滚动速度：每秒 60 像素（可调整，数值越小越慢）
        speed = 60  # 像素/秒
        duration = int(distance / speed * 1000)  # 毫秒

        self.content_widget.move(0, start_y)

        self.animation = QPropertyAnimation(self.content_widget, b"pos")
        self.animation.setDuration(duration)
        self.animation.setStartValue(self.content_widget.pos())
        self.animation.setEndValue(QPoint(0, end_y))
        self.animation.setEasingCurve(QEasingCurve.Linear)
        self.animation.finished.connect(self.accept)
        self.animation.start()

# ---------- 下载等待对话框（强制下载，不可取消）----------
class DownloadDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("正在下载")
        self.setModal(True)
        self.setFixedSize(400, 150)
        self.setWindowFlags(Qt.Dialog | Qt.FramelessWindowHint)
        self.download_thread = None
        self.download_complete = False
        self.save_path = None
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setSpacing(15)
        self.label = QLabel("正在下载游戏启动时的必要资源...\n请勿关闭此窗口")
        self.label.setAlignment(Qt.AlignCenter)
        font = QFont()
        font.setPointSize(11)
        self.label.setFont(font)
        layout.addWidget(self.label)
        self.progress_bar = QProgressBar()
        self.progress_bar.setRange(0, 100)
        layout.addWidget(self.progress_bar)

    def closeEvent(self, event):
        event.ignore()

    def keyPressEvent(self, event):
        if event.key() == Qt.Key_Escape:
            event.ignore()
        else:
            super().keyPressEvent(event)

    def start_download(self, url, save_path):
        self.save_path = save_path
        self.download_thread = DownloadThread(url, save_path)
        self.download_thread.progress.connect(self.progress_bar.setValue)
        self.download_thread.finished.connect(self.on_download_finished)
        self.download_thread.start()

    def on_download_finished(self, success, result):
        self.download_complete = success
        if success:
            self.accept()
        else:
            if self.save_path and os.path.exists(self.save_path):
                try:
                    os.remove(self.save_path)
                except:
                    pass
            QMessageBox.critical(self, "下载失败", f"下载出错：{result}\n即将重试...")
            self.reject()

    def reject(self):
        if not self.download_complete:
            if self.save_path and os.path.exists(self.save_path):
                try:
                    os.remove(self.save_path)
                except:
                    pass
        super().reject()

# ---------- 版本选择对话框（仅展示）----------
VERSIONS = [
    {"display": "v1.0.0 (经典版)", "file": "1.0.0"},
    {"display": "v1.1.0 (效率提升)", "file": "1.1.0"},
    {"display": "v1.2.0 (稳定推荐)", "file": "1.2.0"},
    {"display": "v2.0.0-beta (前瞻版)", "file": "2.0.0-beta"}
]

class VersionDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("选择小猫挖矿版本")
        self.setModal(True)
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setSpacing(12)
        self.list_widget = QListWidget()
        for ver in VERSIONS:
            item = QListWidgetItem(ver["display"])
            self.list_widget.addItem(item)
        layout.addWidget(self.list_widget)
        buttons = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
        buttons.accepted.connect(self.accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)
        self.resize(300, 250)

# ---------- 样式表 ----------
LIGHT_STYLE = """
QMainWindow {
    background-color: #f0f2f5;
}
QWidget#centralWidget {
    background-color: #f0f2f5;
}
QLabel {
    color: #212529;
}
QPushButton {
    background-color: #f57c00;
    color: white;
    border: none;
    border-radius: 48px;
    padding: 8px 16px;
    font-weight: bold;
    font-size: 14px;
}
QPushButton:hover {
    background-color: #e66a00;
}
QPushButton:pressed {
    background-color: #cc5c00;
}
QListWidget {
    border-radius: 16px;
    background-color: white;
    border: 1px solid #ddd;
}
QListWidget::item {
    padding: 10px;
    border-radius: 30px;
}
QListWidget::item:hover {
    background-color: #f57c00;
    color: white;
}
QComboBox, QLineEdit {
    background-color: white;
    border: 1px solid #ccc;
    border-radius: 28px;
    padding: 4px 12px;
}
QFrame#settingsCard {
    background-color: white;
    border-radius: 32px;
    padding: 20px;
    max-width: 500px;
}
"""

# ---------- 启动页面 ----------
class LaunchPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setAlignment(Qt.AlignCenter)
        layout.setSpacing(25)
        self.version_label = QLabel("当前选择版本: v1.0.0 (经典版)")
        self.version_label.setAlignment(Qt.AlignCenter)
        font = QFont()
        font.setPointSize(12)
        self.version_label.setFont(font)
        layout.addWidget(self.version_label)
        self.launch_btn = QPushButton("🚀 启动游戏")
        self.launch_btn.setFixedSize(220, 50)
        layout.addWidget(self.launch_btn, alignment=Qt.AlignCenter)
        self.select_btn = QPushButton("⚙️ 选择版本")
        self.select_btn.setFixedSize(220, 50)
        self.select_btn.clicked.connect(self.show_version_dialog)
        layout.addWidget(self.select_btn, alignment=Qt.AlignCenter)

    def show_version_dialog(self):
        dialog = VersionDialog(self)
        dialog.exec_()

# ---------- 下载页面 ----------
class DownloadPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.init_ui()

    def init_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(20, 20, 20, 20)
        title_label = QLabel("📦 小猫挖矿 · 版本库")
        title_label.setStyleSheet("font-size: 24px; font-weight: bold; color: #f57c00;")
        sub_label = QLabel("下方版本列表仅为展示，下载按钮无任何功能")
        sub_label.setStyleSheet("color: #6c757d; margin-bottom: 16px;")
        main_layout.addWidget(title_label, alignment=Qt.AlignCenter)
        main_layout.addWidget(sub_label, alignment=Qt.AlignCenter)
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QFrame.NoFrame)
        content_widget = QWidget()
        scroll.setWidget(content_widget)
        main_layout.addWidget(scroll)
        scroll_layout = QVBoxLayout(content_widget)
        scroll_layout.setSpacing(12)
        scroll_layout.setAlignment(Qt.AlignTop)
        for ver in VERSIONS:
            row = QWidget()
            row.setObjectName("downloadRow")
            row.setStyleSheet("""
                QWidget#downloadRow {
                    background-color: #f57c00;
                    border-radius: 60px;
                    border: none;
                }
                QWidget#downloadRow:hover {
                    background-color: #e66a00;
                }
            """)
            row_layout = QHBoxLayout(row)
            row_layout.setContentsMargins(20, 12, 20, 12)
            name_label = QLabel(f"🐱 小猫挖矿 {ver['display']}")
            name_label.setStyleSheet("font-weight: 600; font-size: 15px; color: black;")
            row_layout.addWidget(name_label)
            download_btn = QPushButton("⬇️ 下载")
            download_btn.setFixedSize(80, 32)
            download_btn.setStyleSheet("""
                QPushButton {
                    background-color: white;
                    color: #f57c00;
                    border-radius: 30px;
                    font-weight: 500;
                }
                QPushButton:hover {
                    background-color: #fff1e0;
                }
            """)
            row_layout.addWidget(download_btn)
            scroll_layout.addWidget(row)
        footnote = QLabel("所有版本均为独立安装包，直接运行即可开始挖矿之旅 ⛏️")
        footnote.setStyleSheet("font-size: 12px; color: #adb5bd; margin-top: 20px;")
        footnote.setAlignment(Qt.AlignCenter)
        main_layout.addWidget(footnote)

# ---------- 设置页面 ----------
class SettingsPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.config = ConfigManager()
        self.init_ui()
        self.load_settings()

    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setAlignment(Qt.AlignCenter)
        card = QFrame()
        card.setObjectName("settingsCard")
        card.setStyleSheet("""
            QFrame#settingsCard {
                background-color: white;
                border-radius: 32px;
                padding: 20px;
                max-width: 500px;
            }
        """)
        card_layout = QVBoxLayout(card)
        title = QLabel("⚙️ 启动器设置")
        title.setStyleSheet("font-size: 26px; font-weight: bold; color: #f57c00;")
        card_layout.addWidget(title)
        line = QFrame()
        line.setFrameShape(QFrame.HLine)
        line.setStyleSheet("background-color: #eee; max-height: 1px;")
        card_layout.addWidget(line)
        form = QFormLayout()
        form.setSpacing(15)
        form.setLabelAlignment(Qt.AlignLeft)
        self.exit_confirm_cb = QCheckBox("退出前确认")
        form.addRow("🚪 关闭确认:", self.exit_confirm_cb)
        self.game_root_edit = QLineEdit()
        self.game_root_edit.setPlaceholderText("选择游戏根目录（将在此目录下创建 .catcraft/version）")
        browse_btn = QPushButton("浏览...")
        browse_btn.setFixedWidth(80)
        browse_btn.clicked.connect(self.browse_game_root)
        dir_layout = QHBoxLayout()
        dir_layout.addWidget(self.game_root_edit)
        dir_layout.addWidget(browse_btn)
        form.addRow("📁 游戏根目录:", dir_layout)
        card_layout.addLayout(form)
        save_btn = QPushButton("保存设置")
        save_btn.setFixedHeight(45)
        save_btn.clicked.connect(self.save_settings)
        card_layout.addWidget(save_btn)
        layout.addWidget(card, alignment=Qt.AlignCenter)

    def browse_game_root(self):
        directory = QFileDialog.getExistingDirectory(self, "选择游戏根目录")
        if directory:
            self.game_root_edit.setText(directory)

    def load_settings(self):
        self.exit_confirm_cb.setChecked(self.config.get("tips_at_close", True))
        game_root = self.config.get("game_root_dir", CURRENT_DIR)
        self.game_root_edit.setText(game_root)

    def save_settings(self):
        self.config.set("tips_at_close", self.exit_confirm_cb.isChecked())
        new_root = self.game_root_edit.text().strip()
        if new_root:
            self.config.set("game_root_dir", new_root)
        QMessageBox.information(self, "设置已保存", "设置已保存")

    def get_exit_confirm(self):
        return self.config.get("tips_at_close", True)

# ---------- 更多页面 ----------
class MorePage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.config = ConfigManager()
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setAlignment(Qt.AlignCenter)
        layout.setSpacing(30)
        version_label = QLabel(f"版本号：{APP_VERSION}")
        version_label.setAlignment(Qt.AlignCenter)
        version_font = QFont()
        version_font.setPointSize(16)
        version_font.setBold(True)
        version_label.setFont(version_font)
        layout.addWidget(version_label)
        credits_btn = QPushButton("📜 贡献名单")
        credits_btn.setFixedSize(200, 50)
        credits_btn.clicked.connect(self.show_credits)
        layout.addWidget(credits_btn, alignment=Qt.AlignCenter)

    def show_credits(self):
        json_path = self.config.get_creator_json_path()
        def on_download_finished(success, msg):
            if success:
                try:
                    with open(json_path, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                    self.show_credits_dialog(data)
                except Exception as e:
                    QMessageBox.warning(self, "错误", f"解析贡献名单失败：{str(e)}")
            else:
                if os.path.exists(json_path):
                    try:
                        with open(json_path, 'r', encoding='utf-8') as f:
                            data = json.load(f)
                        self.show_credits_dialog(data)
                    except:
                        QMessageBox.warning(self, "错误", f"下载贡献名单失败：{msg}\n且本地文件无效。")
                else:
                    QMessageBox.warning(self, "错误", f"下载贡献名单失败：{msg}")

        self.download_thread = SimpleDownloadThread(CREATOR_JSON_URL, json_path)
        self.download_thread.finished.connect(on_download_finished)
        self.download_thread.start()

    def show_credits_dialog(self, data):
        dialog = CreditsDialog(data, self)
        dialog.exec_()

# ---------- 主窗口 ----------
class CatCraftLauncher(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(f"CatCraft Launcher - {APP_VERSION}")
        self.setMinimumSize(800, 550)
        self.resize(900, 600)
        self.config = ConfigManager()
        self.check_and_download_game()

    def check_and_download_game(self):
        exe_path = self.config.get_game_exe_path()
        if os.path.exists(exe_path):
            self.show_main_window()
        else:
            self.download_game_file()

    def download_game_file(self):
        while True:
            dialog = DownloadDialog()
            exe_path = self.config.get_game_exe_path()
            dialog.start_download(GAME_EXE_URL, exe_path)
            result = dialog.exec_()
            if result == QDialog.Accepted and dialog.download_complete:
                self.show_main_window()
                return
            else:
                continue

    def show_main_window(self):
        self.setup_ui()
        self.apply_style()
        self.show()

    def apply_style(self):
        self.setStyleSheet(LIGHT_STYLE)

    def setup_ui(self):
        central_widget = QWidget()
        central_widget.setObjectName("centralWidget")
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)

        nav_bar = QWidget()
        nav_bar.setFixedHeight(64)
        nav_bar.setStyleSheet("background-color: #f57c00;")
        nav_layout = QHBoxLayout(nav_bar)
        nav_layout.setContentsMargins(20, 0, 20, 0)

        logo = QLabel("CatCraft Launcher")
        logo.setStyleSheet("color: white; font-weight: bold; font-size: 18px; background-color: rgba(0,0,0,0.1); padding: 6px 12px; border-radius: 32px;")
        nav_layout.addWidget(logo)

        btn_container = QWidget()
        btn_layout = QHBoxLayout(btn_container)
        btn_layout.setSpacing(30)
        btn_layout.setContentsMargins(0, 0, 0, 0)

        self.launch_nav_btn = QPushButton("启动")
        self.download_nav_btn = QPushButton("下载")
        self.settings_nav_btn = QPushButton("设置")
        self.more_nav_btn = QPushButton("更多")

        for btn in (self.launch_nav_btn, self.download_nav_btn, self.settings_nav_btn, self.more_nav_btn):
            btn.setFlat(True)
            btn.setStyleSheet("""
                QPushButton {
                    background-color: transparent;
                    color: white;
                    font-weight: 500;
                    border-bottom: 2px solid transparent;
                    border-radius: 0px;
                    padding: 8px 0px;
                }
                QPushButton:hover {
                    border-bottom: 2px solid white;
                }
            """)
            btn_layout.addWidget(btn)

        btn_container.setLayout(btn_layout)
        nav_layout.addWidget(btn_container, alignment=Qt.AlignCenter)

        placeholder = QLabel()
        placeholder.setFixedWidth(160)
        nav_layout.addWidget(placeholder)

        main_layout.addWidget(nav_bar)

        self.stacked_widget = QStackedWidget()
        main_layout.addWidget(self.stacked_widget)

        self.launch_page = LaunchPage(self)
        self.download_page = DownloadPage(self)
        self.settings_page = SettingsPage(self)
        self.more_page = MorePage(self)

        self.stacked_widget.addWidget(self.launch_page)
        self.stacked_widget.addWidget(self.download_page)
        self.stacked_widget.addWidget(self.settings_page)
        self.stacked_widget.addWidget(self.more_page)

        self.launch_nav_btn.clicked.connect(lambda: self.stacked_widget.setCurrentWidget(self.launch_page))
        self.download_nav_btn.clicked.connect(lambda: self.stacked_widget.setCurrentWidget(self.download_page))
        self.settings_nav_btn.clicked.connect(lambda: self.stacked_widget.setCurrentWidget(self.settings_page))
        self.more_nav_btn.clicked.connect(lambda: self.stacked_widget.setCurrentWidget(self.more_page))

        self.stacked_widget.setCurrentWidget(self.launch_page)

    def closeEvent(self, event):
        need_confirm = self.settings_page.get_exit_confirm()
        if need_confirm:
            reply = QMessageBox.question(self, "确认退出", "确定要退出 CatCraft Launcher 吗？",
                                         QMessageBox.Yes | QMessageBox.No, QMessageBox.No)
            if reply == QMessageBox.Yes:
                event.accept()
            else:
                event.ignore()
        else:
            event.accept()

def main():
    app = QApplication(sys.argv)
    app.setStyle(QStyleFactory.create("Fusion"))
    window = CatCraftLauncher()
    sys.exit(app.exec_())

if __name__ == "__main__":
    main()