using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NetworkCore
{
    /// <summary>
    /// 世界传送门连接数据类
    /// 用于存储和验证世界连接的IP地址和端口号信息
    /// </summary>
    [Serializable]
    public class WorldPortalData
    {
        #region 私有字段

        private string ipAddress = "127.0.0.1";
        private int port = 7777;

        #endregion

        #region 公共属性

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress
        {
            get => ipAddress;
            set
            {
                if (IsValidIPv4Address(value))
                {
                    ipAddress = value;
                }
                else
                {
                    Debug.LogWarning($"[WorldPortalData] 无效的IPv4地址格式: {value}");
                }
            }
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get => port;
            set
            {
                if (IsValidPort(value))
                {
                    port = value;
                }
                else
                {
                    Debug.LogWarning($"[WorldPortalData] 无效的端口号: {value}，端口号范围应为1-65535");
                }
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WorldPortalData()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public WorldPortalData(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证IPv4地址格式
        /// </summary>
        /// <param name="ip">待验证的IP地址字符串</param>
        /// <returns>如果是有效的IPv4地址返回true，否则返回false</returns>
        public static bool IsValidIPv4Address(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }

            string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ip, pattern);
        }

        /// <summary>
        /// 验证端口号范围
        /// </summary>
        /// <param name="port">待验证的端口号</param>
        /// <returns>如果端口号在1-65535范围内返回true，否则返回false</returns>
        public static bool IsValidPort(int port)
        {
            return port >= 1 && port <= 65535;
        }

        /// <summary>
        /// 验证当前数据是否有效
        /// </summary>
        /// <returns>如果IP地址和端口号都有效返回true，否则返回false</returns>
        public bool IsValid()
        {
            return IsValidIPv4Address(ipAddress) && IsValidPort(port);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取完整的连接地址字符串（格式：IP:Port）
        /// </summary>
        /// <returns>完整的连接地址字符串</returns>
        public string GetFullAddress()
        {
            return $"{ipAddress}:{port}";
        }

        /// <summary>
        /// 重写ToString方法，返回完整的连接地址
        /// </summary>
        /// <returns>完整的连接地址字符串</returns>
        public override string ToString()
        {
            return GetFullAddress();
        }

        /// <summary>
        /// 克隆当前对象
        /// </summary>
        /// <returns>新的WorldPortalData对象副本</returns>
        public WorldPortalData Clone()
        {
            return new WorldPortalData(ipAddress, port);
        }

        #endregion
    }
}
