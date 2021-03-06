using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;



namespace Victorina
{
    public class AvatarManager : MonoBehaviour
    {
        public static AvatarManager Instance;

        [SerializeField] private GameObject _fileListPan;
        [SerializeField] private GameObject _filesContent;
        [SerializeField] private GameObject _filePrefab;

        [SerializeField] private RawImage _avatarImg;
        [SerializeField] private Text _errorInfo;

        private string _pathToUserAvatar;

        private DirectoryInfo _dirInfo;
        private FileInfo[] _files;
        [SerializeField] private FileScript[] _placeToImage;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (PlayerPrefs.HasKey("AvatarUrl"))
                _pathToUserAvatar = PlayerPrefs.GetString("AvatarUrl");
            WWW www = new WWW(_pathToUserAvatar);
            _avatarImg.texture = www.texture;
        }

        private void OnEnable()
        {
            LoadAvatarList();
        }


        public async void LoadAvatarList()
        {
            string[] dirs = {
               "/mnt/sdcard/Pictures",
               "/mnt/sdcard/Download",
               "/mnt/sdcard/DCIM"
            };

            string res = "????????? ????\n";
            foreach (string s in dirs)
            {
                if (Directory.Exists(s))
                {
                    res += s + "\n";
                }
            }
            _errorInfo.text = res;



            //dir = Path.GetDirectoryName(dir);
            var dir = "/mnt/sdcard/Download";

            if (!Directory.Exists(dir))
            {
                dir = Directory.GetCurrentDirectory();
                if (!Directory.Exists(dir))
                {
                    _errorInfo.text = "???? ?? ??????";
                    return;
                }

            }

            _dirInfo = new DirectoryInfo(dir);

            _errorInfo.text = "????????";

            await Task.Run(() => FileListCreated());

            StartCoroutine(LoadTextures());

        }

        private Task FileListCreated()
        {

            _files = new string[] { "*.png", "*.jpg" }.SelectMany(ext => _dirInfo.GetFiles(ext, SearchOption.AllDirectories)).ToArray();

            return Task.CompletedTask;
        }

        private IEnumerator LoadTextures()
        {
            var cnt = 0;
            var maxCount = _placeToImage.Length;
            var currentIndex = 0;
            while (cnt < _files.Length && currentIndex < maxCount)
            {
                FileInfo f = _files[cnt];

                //if (f.Length < 10000)
                //{
                //    cnt++;
                //    print(f.Name + "  " + f.Length);
                //    continue;
                //}

                var request = UnityWebRequestTexture.GetTexture("file://" + _files[cnt].FullName);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _placeToImage[currentIndex].Index = cnt;
                    _placeToImage[currentIndex].Image.texture = DownloadHandlerTexture.GetContent(request);
                    _placeToImage[currentIndex].gameObject.SetActive(true);
                    currentIndex++;
                }
                else
                    _errorInfo.text = request.error;

                request.Dispose();
                if (cnt > 40) yield break;
                cnt++;

            }
            _errorInfo.text = "?????????";
        }

        public void SelectAvatar(int index)
        {
            WWW www = new WWW("file://" + _files[index].FullName);
            _avatarImg.texture = www.texture;
            _fileListPan.SetActive(false);

            _pathToUserAvatar = www.url;
            PlayerPrefs.SetString("AvatarUrl", _pathToUserAvatar);

            DestroyTempFiles();
        }

        public void DestroyTempFiles()
        {
            if (_placeToImage.Length > 0)
                foreach (var obj in _placeToImage)
                    obj.gameObject.SetActive(false);
        }
    }

}