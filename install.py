import os
import sys

NAME = ""
TOKEN = ""

EXECUTABLE_PATH = ""
SERVICE_PATH = ""
USER = "root"

SERVICE_CONTENT = f"""[Unit]
Description={NAME} telegram bot
After=network.target

[Service]
ExecStart={EXECUTABLE_PATH}/MyApp --token={TOKEN}
WorkingDirectory={EXECUTABLE_PATH}
Restart=always
User=**usr**
Group=user

[Install]
WantedBy=multi-user.target
"""

def load_args():
    global NAME, TOKEN, EXECUTABLE_PATH, SERVICE_PATH
    for i, arg in enumerate(sys.argv):
        if arg == "--name":
            NAME = f"tgbot_{sys.argv[i + 1]}"
        if arg == "--token":
            TOKEN = sys.argv[i + 1]

    EXECUTABLE_PATH = f"/usr/local/bin/{NAME}"
    SERVICE_PATH = f"/etc/systemd/system/{NAME}.service"

def check_args():
    if NAME == "":
        print("You need to specify the service name by using --name")
    if TOKEN == "":
        print(TOKEN)
        print("You need to specify the service token by using --token")
        sys.exit(1)

def copy_bin_files():
    if os.path.exists(EXECUTABLE_PATH):
        os.system(f"rm -rf {EXECUTABLE_PATH}")

    os.system(f"cp -r bin/Release/* {EXECUTABLE_PATH}")
    print(f"Бинарные файлы скопированы в {EXECUTABLE_PATH}")


def generate_service_content():
    with open(SERVICE_PATH, "w") as file:
        file.write(SERVICE_CONTENT)

    os.chmod(SERVICE_PATH, 0o644)
    print(f"Сервисный файл создан: {SERVICE_PATH}")
    print("Не забудьте выполнить:")
    print(f"  sudo systemctl daemon-reload")
    print(f"  sudo systemctl enable {NAME}.service")
    print(f"  sudo systemctl start {NAME}.service")

if __name__ == "__main__":
    load_args()
    check_args()
    copy_bin_files()
    #generate_service_content()