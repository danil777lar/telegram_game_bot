import os
import sys

NAME = ""
TOKEN = ""
GAME = ""

EXECUTABLE_PATH = ""
SERVICE_PATH = ""
USER = "root"

def load_args():
    global NAME, TOKEN, GAME, EXECUTABLE_PATH, SERVICE_PATH
    for i, arg in enumerate(sys.argv):
        if arg == "--name":
            NAME = f"tgbot_{sys.argv[i + 1]}"
        if arg == "--token":
            TOKEN = sys.argv[i + 1]
        if arg == "--game":
            GAME = sys.argv[i + 1]

    EXECUTABLE_PATH = f"/usr/local/bin/{NAME}"
    SERVICE_PATH = f"/etc/systemd/system/{NAME}.service"

def check_args():
    need_exit = False
    if NAME == "":
        print("You need to specify the service name by using --name")
        need_exit = True
    if TOKEN == "":
        print("You need to specify the service token by using --token")
        need_exit = True
    if GAME == "":
        print("You need to specify the service game by using --game")
        need_exit = True

    if need_exit:
        sys.exit(1)

def copy_bin_files():
    if os.path.exists(EXECUTABLE_PATH):
        os.system(f"rm -rf {EXECUTABLE_PATH}")

    os.system(f"cp -r bin/Release/* {EXECUTABLE_PATH}")
    print(f"Бинарные файлы скопированы в {EXECUTABLE_PATH}")


def generate_service_content():
    service_content = f"""
    [Unit]
    Description={NAME} telegram bot
    After=network.target

    [Service]
    ExecStart={EXECUTABLE_PATH}/telegram_game_bot --token={TOKEN} --game={GAME}
    WorkingDirectory={EXECUTABLE_PATH}
    Restart=always
    User={USER}
    Group={USER}

    [Install]
    WantedBy=multi-user.target
    """

    with open(SERVICE_PATH, "w") as file:
        file.write(service_content)

    os.chmod(SERVICE_PATH, 0o644)
    print(f"Сервисный файл создан: {SERVICE_PATH}")
    os.system(f"sudo systemctl daemon-reload")
    os.system(f"sudo systemctl enable {NAME}.service")
    os.system(f"sudo systemctl start {NAME}.service")

if __name__ == "__main__":
    load_args()
    check_args()
    copy_bin_files()
    generate_service_content()