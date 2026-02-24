using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Plugin.SDK.Core;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 绔彛杩炴帴 - 璁板綍婧愯妭鐐圭鍙ｅ埌鐩爣鑺傜偣绔彛鐨勮繛鎺?
    /// </summary>
    public class PortConnection
    {
        /// <summary>
        /// 婧愯妭鐐笽D
        /// </summary>
        public string SourceNodeId { get; set; }

        /// <summary>
        /// 婧愮鍙?(濡? "output", "left", "right", "top", "bottom")
        /// </summary>
        public string SourcePort { get; set; }

        /// <summary>
        /// 鐩爣鑺傜偣ID
        /// </summary>
        public string TargetNodeId { get; set; }

        /// <summary>
        /// 鐩爣绔彛 (濡? "input", "left", "right", "top", "bottom")
        /// </summary>
        public string TargetPort { get; set; }
    }

    /// <summary>
    /// 鎵ц閾?- 鐢盨tart鑺傜偣椹卞姩鐨勭嫭绔嬫墽琛屽崟鍏?
    /// </summary>
    public class ExecutionChain
    {
        /// <summary>
        /// 鎵ц閾惧敮涓€鏍囪瘑
        /// </summary>
        public string ChainId { get; set; }

        /// <summary>
        /// 璧峰鑺傜偣ID
        /// </summary>
        public string StartNodeId { get; set; }

        /// <summary>
        /// 鎵ц閾句腑鐨勮妭鐐笽D鍒楄〃锛堟寜鎷撴墤椤哄簭锛?
        /// </summary>
        public List<string> NodeIds { get; set; }

        /// <summary>
        /// 鎵ц閾句腑鐨勬墍鏈変緷璧栵紙鐢ㄤ簬璺ㄩ摼鍚屾锛?
        /// </summary>
        public List<ChainDependency> Dependencies { get; set; }

        public ExecutionChain()
        {
            NodeIds = new List<string>();
            Dependencies = new List<ChainDependency>();
        }
    }

    /// <summary>
    /// 鎵ц閾句緷璧栧叧绯?
    /// </summary>
    public class ChainDependency
    {
        /// <summary>
        /// 渚濊禆鐨勬簮閾綢D
        /// </summary>
        public string SourceChainId { get; set; }

        /// <summary>
        /// 婧愯妭鐐笽D
        /// </summary>
        public string SourceNodeId { get; set; }

        /// <summary>
        /// 鏈摼涓緷璧栬婧愮殑鑺傜偣ID
        /// </summary>
        public string TargetNodeId { get; set; }
    }

    /// <summary>
    /// Workflow
    /// </summary>
    public class Workflow
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Workflow description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Node list
        /// </summary>
        public List<WorkflowNode> Nodes { get; set; }

        /// <summary>
        /// Node connections (source node ID -> target node ID list)
        /// </summary>
        public Dictionary<string, List<string>> Connections { get; set; }

        /// <summary>
        /// 绔彛绾ц繛鎺ュ垪琛?- 绮剧‘璁板綍鍝釜绔彛杩炴帴鍒板摢涓鍙?
        /// </summary>
        public List<PortConnection> PortConnections { get; set; }

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Set logger (internal use for deserialization)
        /// </summary>
        internal void SetLogger(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Workflow(string id, string name, ILogger logger)
        {
            Id = id;
            Name = name;
            Logger = logger;
            Nodes = new List<WorkflowNode>();
            Connections = new Dictionary<string, List<string>>();
            PortConnections = new List<PortConnection>();
        }

        /// <summary>
        /// 娣诲姞鑺傜偣鍒板伐浣滄祦
        /// </summary>
        /// <param name="node">瑕佹坊鍔犵殑宸ヤ綔娴佽妭鐐?/param>
        public void AddNode(WorkflowNode node)
        {
            Nodes.Add(node);
            Logger.LogInfo($"Workflow {Name} added node: {node.Name}");
        }

        /// <summary>
        /// 浠庡伐浣滄祦涓Щ闄よ妭鐐?
        /// </summary>
        /// <param name="nodeId">瑕佺Щ闄ょ殑鑺傜偣ID</param>
        /// <remarks>
        /// 绉婚櫎鎿嶄綔鍖呮嫭锛?
        /// 1. 浠庤妭鐐瑰垪琛ㄤ腑绉婚櫎
        /// 2. 绉婚櫎璇ヨ妭鐐圭殑鎵€鏈夎緭鍑鸿繛鎺?
        /// 3. 绉婚櫎鍏朵粬鑺傜偣鍒拌鑺傜偣鐨勮緭鍏ヨ繛鎺?
        /// 4. 绉婚櫎璇ヨ妭鐐圭殑鎵€鏈夌鍙ｈ繛鎺?
        /// </remarks>
        public void RemoveNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                Nodes.Remove(node);
                Connections.Remove(nodeId);

                // 绉婚櫎鍏朵粬鑺傜偣鍒拌鑺傜偣鐨勮繛鎺?
                foreach (var kvp in Connections)
                {
                    kvp.Value.Remove(nodeId);
                }

                // 绉婚櫎璇ヨ妭鐐圭殑绔彛杩炴帴
                PortConnections.RemoveAll(pc => pc.SourceNodeId == nodeId || pc.TargetNodeId == nodeId);

                Logger.LogInfo($"Workflow {Name} removed node: {node.Name}");
            }
        }

        /// <summary>
        /// 杩炴帴涓や釜鑺傜偣锛堝垱寤轰粠婧愯妭鐐瑰埌鐩爣鑺傜偣鐨勮繛鎺ワ級
        /// </summary>
        /// <param name="sourceNodeId">婧愯妭鐐笽D</param>
        /// <param name="targetNodeId">鐩爣鑺傜偣ID</param>
        public void ConnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (!Connections.ContainsKey(sourceNodeId))
            {
                Connections[sourceNodeId] = new List<string>();
            }

            if (!Connections[sourceNodeId].Contains(targetNodeId))
            {
                Connections[sourceNodeId].Add(targetNodeId);
                Logger.LogInfo($"Workflow {Name} connected nodes: {sourceNodeId} -> {targetNodeId}");
            }
        }

        /// <summary>
        /// Connect nodes by port - 绮剧‘鎸囧畾婧愮鍙ｅ拰鐩爣绔彛
        /// </summary>
        public void ConnectNodesByPort(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            // 娣诲姞鑺傜偣绾ц繛鎺?
            ConnectNodes(sourceNodeId, targetNodeId);

            // 娣诲姞绔彛绾ц繛鎺?
            var existingConnection = PortConnections.FirstOrDefault(pc =>
                pc.SourceNodeId == sourceNodeId && pc.SourcePort == sourcePort &&
                pc.TargetNodeId == targetNodeId && pc.TargetPort == targetPort);

            if (existingConnection == null)
            {
                PortConnections.Add(new PortConnection
                {
                    SourceNodeId = sourceNodeId,
                    SourcePort = sourcePort,
                    TargetNodeId = targetNodeId,
                    TargetPort = targetPort
                });

                Logger.LogInfo($"Workflow {Name} connected ports: {sourceNodeId}.{sourcePort} -> {targetNodeId}.{targetPort}");
            }
        }

        /// <summary>
        /// Disconnect nodes by port
        /// </summary>
        public void DisconnectNodesByPort(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            // 绉婚櫎鑺傜偣绾ц繛鎺?
            DisconnectNodes(sourceNodeId, targetNodeId);

            // 绉婚櫎绔彛绾ц繛鎺?
            var connection = PortConnections.FirstOrDefault(pc =>
                pc.SourceNodeId == sourceNodeId && pc.SourcePort == sourcePort &&
                pc.TargetNodeId == targetNodeId && pc.TargetPort == targetPort);

            if (connection != null)
            {
                PortConnections.Remove(connection);
                Logger.LogInfo($"Workflow {Name} disconnected ports: {sourceNodeId}.{sourcePort} -> {targetNodeId}.{targetPort}");
            }
        }

        /// <summary>
        /// 鑾峰彇鑺傜偣鐨勫苟琛屾墽琛屽垎缁勶紙鍩轰簬鎷撴墤鎺掑簭鐨勫垎灞傦級
        /// </summary>
        /// <returns>骞惰鎵ц缁勫垪琛紝姣忎釜缁勫寘鍚彲骞惰鎵ц鐨勮妭鐐笽D</returns>
        /// <remarks>
        /// 绠楁硶璇存槑锛?
        /// 1. 鍒濆鍖栵細鎵€鏈夎妭鐐规湭澶勭悊锛屽凡澶勭悊闆嗗悎涓虹┖
        /// 2. 杩唬锛氬湪姣忚疆涓紝鎵惧嚭鎵€鏈変緷璧栧凡婊¤冻鐨勮妭鐐?
        /// 3. 鍒嗙粍锛氬皢婊¤冻鏉′欢鐨勮妭鐐瑰姞鍏ュ綋鍓嶅垎缁?
        /// 4. 鏇存柊锛氭爣璁板綋鍓嶇粍涓哄凡澶勭悊锛岀户缁笅涓€杞?
        /// 5. 缁堟锛氭墍鏈夎妭鐐瑰鐞嗗畬姣曟垨鏃犳硶缁х画锛堝惊鐜緷璧栵級
        /// </remarks>
        public List<List<string>> GetParallelExecutionGroups()
        {
            var groups = new List<List<string>>();
            var remaining = new HashSet<string>(Nodes.Select(n => n.Id));
            var processed = new HashSet<string>();

            while (remaining.Count > 0)
            {
                var currentGroup = new List<string>();

                // 鎵惧嚭褰撳墠鎵€鏈変緷璧栧凡婊¤冻鐨勮妭鐐?
                foreach (var nodeId in remaining.ToList())
                {
                    var dependencies = Connections
                        .Where(kvp => kvp.Value.Contains(nodeId))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    if (dependencies.All(dep => processed.Contains(dep)))
                    {
                        currentGroup.Add(nodeId);
                    }
                }

                // 濡傛灉鏃犳硶鎵惧埌鍙墽琛岀殑鑺傜偣锛岃鏄庡瓨鍦ㄥ惊鐜緷璧?
                if (currentGroup.Count == 0)
                {
                    Logger?.LogWarning($"鏃犳硶缁х画鍒嗙粍锛屽彲鑳藉瓨鍦ㄥ惊鐜緷璧?);
                    break;
                }

                groups.Add(currentGroup);

                // 鏍囪褰撳墠缁勪负宸插鐞?
                foreach (var nodeId in currentGroup)
                {
                    processed.Add(nodeId);
                    remaining.Remove(nodeId);
                }
            }

            return groups;
        }

        /// <summary>
        /// 鑾峰彇鑺傜偣鐨勬墽琛岄『搴忥紙浣跨敤Kahn绠楁硶杩涜鎷撴墤鎺掑簭锛?
        /// </summary>
        /// <returns>鎸夋嫇鎵戞帓搴忕殑鑺傜偣ID鍒楄〃</returns>
        /// <remarks>
        /// 鎷撴墤鎺掑簭绠楁硶姝ラ锛?
        /// 1. 璁＄畻姣忎釜鑺傜偣鐨勫叆搴︼紙鎸囧悜璇ヨ妭鐐圭殑杩炴帴鏁伴噺锛?
        /// 2. 灏嗘墍鏈夊叆搴︿负0鐨勮妭鐐瑰姞鍏ラ槦鍒?
        /// 3. 浠庨槦鍒椾腑鍙栧嚭鑺傜偣骞跺姞鍏ョ粨鏋滃垪琛?
        /// 4. 鍑忓皯璇ヨ妭鐐规墍鏈夊悗缁ц妭鐐圭殑鍏ュ害锛屽鏋滃叆搴﹀彉涓?鍒欏姞鍏ラ槦鍒?
        /// 5. 閲嶅姝ラ3-4鐩村埌闃熷垪涓虹┖
        /// 6. 濡傛灉澶勭悊鐨勮妭鐐规暟涓嶇瓑浜庢€昏妭鐐规暟锛岃鏄庡瓨鍦ㄥ惊鐜緷璧?
        /// </remarks>
        public List<string> GetExecutionOrder()
        {
            var order = new List<string>();
            var inDegree = CalculateNodeInDegrees();

            // 灏嗗叆搴︿负0鐨勮妭鐐瑰姞鍏ラ槦鍒?
            var queue = new Queue<string>();
            foreach (var node in Nodes)
            {
                if (inDegree[node.Id] == 0)
                {
                    queue.Enqueue(node.Id);
                }
            }

            // 鎷撴墤鎺掑簭澶勭悊
            var processedCount = 0;
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                order.Add(nodeId);
                processedCount++;

                // 鍑忓皯鍚庣户鑺傜偣鐨勫叆搴?
                ProcessSuccessors(nodeId, inDegree, queue);
            }

            // 妫€娴嬪惊鐜緷璧?
            if (processedCount != Nodes.Count)
            {
                Logger?.LogWarning($"鎷撴墤鎺掑簭妫€娴嬪埌寰幆渚濊禆锛屽凡澶勭悊 {processedCount}/{Nodes.Count} 涓妭鐐?);
            }

            Logger?.LogInfo($"鎷撴墤鎺掑簭瀹屾垚锛屾墽琛岄『搴? [{string.Join(" -> ", order)}]");
            return order;
        }

        /// <summary>
        /// 澶勭悊鎸囧畾鑺傜偣鐨勬墍鏈夊悗缁ц妭鐐癸紝鍑忓皯瀹冧滑鐨勫叆搴?
        /// </summary>
        /// <param name="nodeId">婧愯妭鐐笽D</param>
        /// <param name="inDegree">鑺傜偣鍏ュ害瀛楀吀</param>
        /// <param name="queue">寰呭鐞嗚妭鐐归槦鍒?/param>
        private void ProcessSuccessors(string nodeId, Dictionary<string, int> inDegree, Queue<string> queue)
        {
            if (!Connections.ContainsKey(nodeId))
            {
                return;
            }

            foreach (var dependentId in Connections[nodeId])
            {
                inDegree[dependentId]--;
                if (inDegree[dependentId] == 0)
                {
                    queue.Enqueue(dependentId);
                }
            }
        }

        /// <summary>
        /// 妫€娴嬪伐浣滄祦涓殑寰幆渚濊禆
        /// </summary>
        /// <returns>妫€娴嬪埌鐨勫惊鐜緷璧栬矾寰勫垪琛?/returns>
        /// <remarks>
        /// 浣跨敤DFS娣卞害浼樺厛鎼滅储绠楁硶妫€娴嬪惊鐜細
        /// 1. 璁块棶鑺傜偣鏃跺姞鍏ュ凡璁块棶闆嗗悎鍜岄€掑綊鏍?
        /// 2. 閫掑綊璁块棶鎵€鏈夊悗缁ц妭鐐?
        /// 3. 濡傛灉鍦ㄩ€掑綊鏍堜腑鍐嶆閬囧埌鏌愪釜鑺傜偣锛岃鏄庡瓨鍦ㄥ惊鐜?
        /// 4. 璁板綍寰幆璺緞骞惰繑鍥?
        /// </remarks>
        public List<string> DetectCycles()
        {
            var cycles = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    DetectCycleDFS(node.Id, node.Id, cycles, visited, recursionStack);
                }
            }

            return cycles;
        }

        /// <summary>
        /// 浣跨敤娣卞害浼樺厛鎼滅储锛圖FS锛夋娴嬪惊鐜緷璧?
        /// </summary>
        /// <param name="nodeId">褰撳墠鑺傜偣ID</param>
        /// <param name="path">褰撳墠璺緞</param>
        /// <param name="cycles">寰幆鍒楄〃锛堢敤浜庤褰曞彂鐜扮殑寰幆锛?/param>
        /// <param name="visited">宸茶闂妭鐐归泦鍚?/param>
        /// <param name="recursionStack">閫掑綊鏍堬紙鐢ㄤ簬妫€娴嬪洖杈癸級</param>
        /// <returns>鏄惁鍙戠幇寰幆</returns>
        private bool DetectCycleDFS(
            string nodeId,
            string path,
            List<string> cycles,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            if (Connections.ContainsKey(nodeId))
            {
                foreach (var dependentId in Connections[nodeId])
                {
                    if (!visited.Contains(dependentId))
                    {
                        if (DetectCycleDFS(dependentId, path + " -> " + dependentId, cycles, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(dependentId))
                    {
                        // 鍙戠幇鍥炶竟锛岃褰曞惊鐜矾寰?
                        cycles.Add(path + " -> " + dependentId);
                        return true;
                    }
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        /// <summary>
        /// 鑾峰彇鑺傜偣浼樺厛绾э紙鍩轰簬浠庤鑺傜偣鍑哄彂鐨勬渶闀胯矾寰勯暱搴︼級
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>鑺傜偣浼樺厛绾у€硷紙鍊艰秺澶ц〃绀鸿鑺傜偣绂诲嚭鍙ｈ秺杩滐級</returns>
        /// <remarks>
        /// 浼樺厛绾ц绠楄鏄庯細
        /// 1. 鍑哄彛鑺傜偣锛堟棤鍚庣户鑺傜偣锛夌殑浼樺厛绾т负0
        /// 2. 鍏朵粬鑺傜偣鐨勪紭鍏堢骇 = 1 + max(鎵€鏈夊悗缁ц妭鐐圭殑浼樺厛绾?
        /// 3. 浼樺厛绾ц秺楂樼殑鑺傜偣搴旇瓒婃棭鎵ц
        /// </remarks>
        public int GetNodePriority(string nodeId)
        {
            var visited = new HashSet<string>();
            return CalculateNodePriority(nodeId, visited);
        }

        /// <summary>
        /// 閫掑綊璁＄畻鑺傜偣浼樺厛绾?
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <param name="visited">宸茶闂妭鐐归泦鍚堬紙鐢ㄤ簬閬垮厤寰幆瀵艰嚧鐨勬棤闄愰€掑綊锛?/param>
        /// <returns>鑺傜偣浼樺厛绾?/returns>
        private int CalculateNodePriority(string nodeId, HashSet<string> visited)
        {
            // 閬垮厤寰幆瀵艰嚧鐨勬棤闄愰€掑綊
            if (visited.Contains(nodeId))
            {
                return 0;
            }

            visited.Add(nodeId);

            // 鍑哄彛鑺傜偣浼樺厛绾т负0
            if (!Connections.ContainsKey(nodeId))
            {
                return 0;
            }

            // 璁＄畻鎵€鏈夊悗缁ц妭鐐圭殑鏈€澶т紭鍏堢骇
            var maxPriority = 0;
            foreach (var dependentId in Connections[nodeId])
            {
                var priority = CalculateNodePriority(dependentId, visited);
                if (priority > maxPriority)
                {
                    maxPriority = priority;
                }
            }

            return maxPriority + 1;
        }

        /// <summary>
        /// Disconnect nodes
        /// </summary>
        public void DisconnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (Connections.ContainsKey(sourceNodeId))
            {
                Connections[sourceNodeId].Remove(targetNodeId);
                Logger.LogInfo($"Workflow {Name} disconnected: {sourceNodeId} -> {targetNodeId}");
            }
        }

        /// <summary>
        /// 鎵ц宸ヤ綔娴?
        /// </summary>
        /// <param name="inputImage">杈撳叆鍥惧儚鏁版嵁</param>
        /// <returns>鎵€鏈夎妭鐐圭殑鎵ц缁撴灉鍒楄〃</returns>
        /// <remarks>
        /// 鎵ц娴佺▼锛?
        /// 1. 鍒涘缓鎵ц璁″垝锛堝苟琛屽垎缁勶級
        /// 2. 鑾峰彇鎷撴墤鎺掑簭鐨勬墽琛岄『搴?
        /// 3. 鎸夐『搴忎緷娆℃墽琛屾瘡涓妭鐐?
        /// 4. 鑺傜偣鐨勮緭鍑轰綔涓哄悗缁妭鐐圭殑杈撳叆
        /// 5. 璁板綍鎵€鏈夎妭鐐圭殑鎵ц缁撴灉鍜屾墽琛屾椂闂?
        /// </remarks>
        public List<AlgorithmResult> Execute(Mat inputImage)
        {
            var results = new List<AlgorithmResult>();
            var nodeResults = new Dictionary<string, Mat>();
            var executedNodes = new HashSet<string>();

            Logger.LogInfo($"Starting workflow execution: {Name}");

            // 鍒涘缓鎵ц璁″垝
            var executionPlan = CreateExecutionPlan();
            executionPlan.Start();

            // 鑾峰彇鎷撴墤鎺掑簭鐨勬墽琛岄『搴?
            var executionOrder = GetExecutionOrder();
            if (executionOrder.Count == 0)
            {
                Logger.LogWarning("鏃犳硶纭畾鎵ц椤哄簭,鍙兘瀛樺湪寰幆渚濊禆");
                return results;
            }

            // 鎸夋墽琛岄『搴忔墽琛岃妭鐐?
            foreach (var nodeId in executionOrder)
            {
                var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node != null)
                {
                    ExecuteNode(node, inputImage, nodeResults, executedNodes, results);
                }
            }

            executionPlan.Complete();
            Logger.LogInfo(executionPlan.GetReport());

            return results;
        }

        /// <summary>
        /// 鍒涘缓鎵ц璁″垝锛堝熀浜庡苟琛屾墽琛屽垎缁勶級
        /// </summary>
        /// <returns>鎵ц璁″垝瀵硅薄</returns>
        /// <remarks>
        /// 鎵ц璁″垝鍖呭惈锛?
        /// 1. 澶氫釜鎵ц缁勶紝姣忎釜缁勫寘鍚彲骞惰鎵ц鐨勮妭鐐?
        /// 2. 鎵ц缁勭殑搴忓彿鍜岀姸鎬?
        /// 3. 鎵ц寮€濮嬪拰缁撴潫鏃堕棿
        /// </remarks>
        public ExecutionPlan CreateExecutionPlan()
        {
            var plan = new ExecutionPlan();
            var groups = GetParallelExecutionGroups();

            for (int i = 0; i < groups.Count; i++)
            {
                plan.Groups.Add(new ExecutionGroup
                {
                    GroupNumber = i + 1,
                    NodeIds = groups[i],
                    Status = ExecutionGroupStatus.Pending
                });
            }

            return plan;
        }

        /// <summary>
        /// 鎵ц鍗曚釜宸ヤ綔娴佽妭鐐?
        /// </summary>
        /// <param name="node">瑕佹墽琛岀殑鑺傜偣</param>
        /// <param name="inputImage">杈撳叆鍥惧儚</param>
        /// <param name="nodeResults">鑺傜偣鎵ц缁撴灉鏄犲皠琛紙鐢ㄤ簬瀛樺偍鍜岃幏鍙栦腑闂寸粨鏋滐級</param>
        /// <param name="executedNodes">宸叉墽琛岃妭鐐归泦鍚堬紙鐢ㄤ簬閬垮厤閲嶅鎵ц锛?/param>
        /// <param name="results">绠楁硶缁撴灉鍒楄〃锛堢敤浜庢敹闆嗘墍鏈夎妭鐐圭殑鎵ц缁撴灉锛?/param>
        private void ExecuteNode(
            WorkflowNode node,
            Mat inputImage,
            Dictionary<string, Mat> nodeResults,
            HashSet<string> executedNodes,
            List<AlgorithmResult> results)
        {
            // 璺宠繃宸叉墽琛屾垨鏈惎鐢ㄧ殑鑺傜偣
            if (executedNodes.Contains(node.Id) || !node.IsEnabled)
            {
                return;
            }

            // 鍑嗗杈撳叆鏁版嵁
            var input = PrepareNodeInput(node, inputImage, nodeResults);

            try
            {
                Logger.LogInfo($"Executing node: {node.Name} (ID: {node.Id})");

                // 鍒涘缓绠楁硶瀹炰緥骞舵墽琛?
                var algorithm = node.CreateInstance();
                var resultImage = algorithm.Process(input) as Mat;

                // 淇濆瓨鑺傜偣鎵ц缁撴灉
                nodeResults[node.Id] = resultImage ?? input;

                // 璁板綍鎵ц缁撴灉
                var result = new AlgorithmResult
                {
                    AlgorithmName = node.AlgorithmType,
                    Success = true,
                    ResultImage = resultImage,
                    ExecutionTime = DateTime.Now
                };

                results.Add(result);
                executedNodes.Add(node.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to execute node {node.Name}: {ex.Message}", ex);

                results.Add(new AlgorithmResult
                {
                    AlgorithmName = node.AlgorithmType,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 鑾峰彇宸ヤ綔娴佷俊鎭憳瑕?
        /// </summary>
        /// <returns>宸ヤ綔娴佷俊鎭殑鏍煎紡鍖栧瓧绗︿覆</returns>
        /// <remarks>
        /// 淇℃伅鍖呭惈锛?
        /// - 宸ヤ綔娴佸悕绉板拰ID
        /// - 宸ヤ綔娴佹弿杩?
        /// - 鑺傜偣鎬绘暟
        /// - 鎵€鏈夎妭鐐圭殑璇︾粏鍒楄〃锛堝悕绉般€両D銆佺畻娉曠被鍨嬨€佸惎鐢ㄧ姸鎬侊級
        /// </remarks>
        public string GetInfo()
        {
            var info = $"Workflow: {Name} (ID: {Id})\n";
            info += $"Description: {Description}\n";
            info += $"Node count: {Nodes.Count}\n";
            info += "Node list:\n";

            foreach (var node in Nodes)
            {
                var status = node.IsEnabled ? "Enabled" : "Disabled";
                info += $"  - {node.Name} (ID: {node.Id}, {node.AlgorithmType}) - {status}\n";
            }

            return info;
        }

        /// <summary>
        /// 鑷姩璇嗗埆鎵ц閾撅紙鍩轰簬鑺傜偣杩炴帴鍏崇郴鍜屽叆搴︼級
        /// </summary>
        /// <returns>璇嗗埆鍒扮殑鎵ц閾惧垪琛?/returns>
        public List<ExecutionChain> GetAutoDetectExecutionChains()
        {
            var chains = new List<ExecutionChain>();
            var allVisitedNodes = new HashSet<string>();
            var chainCounter = 0;

            // 姝ラ1锛氳绠楁瘡涓妭鐐圭殑鍏ュ害
            var inDegree = CalculateNodeInDegrees();

            // 姝ラ2锛氳瘑鍒墍鏈夊叆鍙ｈ妭鐐癸紙鍏ュ害=0涓斿凡鍚敤锛?
            var entryNodes = Nodes
                .Where(n => inDegree[n.Id] == 0 && n.IsEnabled)
                .ToList();

            Logger?.LogInfo($"[AutoDetect] 鎵惧埌 {entryNodes.Count} 涓叆鍙ｈ妭鐐?);

            // 姝ラ3锛氬叆鍙ｈ妭鐐瑰悎骞?- 灏嗗叡浜笅娓歌妭鐐圭殑鍏ュ彛鑺傜偣鍚堝苟鍒板悓涓€鎵ц閾?
            var entryGroups = GroupEntryNodesByDownstream(entryNodes);

            // 姝ラ4锛氫负姣忎釜鍏ュ彛缁勬瀯寤烘墽琛岄摼
            foreach (var entryGroup in entryGroups)
            {
                var chain = CreateExecutionChain(entryGroup, chainCounter, allVisitedNodes, chains);
                chains.Add(chain);
                chainCounter++;

                var entryNames = string.Join(", ", entryGroup.Select(n => n.Name));
                Logger?.LogInfo($"[AutoDetect] 鍒涘缓鎵ц閾綶{chainCounter}]: [{entryNames}] (鍖呭惈{chain.NodeIds.Count}涓妭鐐?");
            }

            // 姝ラ5锛氬鐞嗘湭璁块棶鐨勮妭鐐癸紙瀛ゅ矝鑺傜偣锛?
            CreateIsolatedChains(allVisitedNodes, ref chainCounter, chains);

            Logger?.LogInfo($"[AutoDetect] 鍏辫瘑鍒?{chains.Count} 鏉℃墽琛岄摼");
            return chains;
        }

        /// <summary>
        /// 鑾峰彇鎵ц閾撅紙鍩轰簬杩炴帴鍏崇郴鑷姩璇嗗埆鍏ュ彛鑺傜偣锛?
        /// </summary>
        /// <returns>鎵ц閾惧垪琛?/returns>
        public List<ExecutionChain> GetExecutionChains()
        {
            return GetAutoDetectExecutionChains();
        }

        /// <summary>
        /// 閫掑綊鏀堕泦鎵ц閾句腑鐨勬墍鏈夎妭鐐癸紙浠庢寚瀹氳妭鐐瑰紑濮嬶級
        /// </summary>
        /// <param name="nodeId">璧峰鑺傜偣ID</param>
        /// <param name="chainNodes">鎵ц閾捐妭鐐瑰垪琛紙鐢ㄤ簬鏀堕泦鑺傜偣锛?/param>
        /// <param name="allVisitedNodes">鍏ㄥ眬宸茶闂妭鐐归泦鍚堬紙鐢ㄤ簬閬垮厤璺ㄩ摼閲嶅璁块棶锛?/param>
        /// <remarks>
        /// 绠楁硶璇存槑锛?
        /// 1. 妫€鏌ヨ妭鐐规槸鍚﹀凡璁块棶锛堥伩鍏嶉噸澶嶅拰寰幆锛?
        /// 2. 灏嗚妭鐐瑰姞鍏ユ墽琛岄摼鍜屽叏灞€璁块棶闆嗗悎
        /// 3. 閫掑綊鏀堕泦鎵€鏈変笅娓歌妭鐐?
        /// </remarks>
        private void CollectExecutionChain(
            string nodeId,
            List<string> chainNodes,
            HashSet<string> allVisitedNodes)
        {
            // 璺宠繃宸茶闂殑鑺傜偣
            if (allVisitedNodes.Contains(nodeId) || chainNodes.Contains(nodeId))
            {
                return;
            }

            // 娣诲姞鑺傜偣鍒版墽琛岄摼鍜屽叏灞€璁块棶闆嗗悎
            chainNodes.Add(nodeId);
            allVisitedNodes.Add(nodeId);

            // 閫掑綊鏀堕泦涓嬫父鑺傜偣
            if (Connections.ContainsKey(nodeId))
            {
                foreach (var childId in Connections[nodeId])
                {
                    CollectExecutionChain(childId, chainNodes, allVisitedNodes);
                }
            }
        }

        /// <summary>
        /// 鍒嗘瀽骞惰褰曟墽琛岄摼鐨勮法閾句緷璧栧叧绯?
        /// </summary>
        /// <param name="chain">鐩爣鎵ц閾?/param>
        /// <param name="allVisitedNodes">鍏ㄥ眬宸茶闂妭鐐归泦鍚?/param>
        /// <param name="existingChains">宸插瓨鍦ㄧ殑鎵ц閾惧垪琛?/param>
        /// <remarks>
        /// 璺ㄩ摼渚濊禆瀹氫箟锛?
        /// - 褰撴墽琛岄摼涓殑鏌愪釜鑺傜偣渚濊禆浜庡叾浠栨墽琛岄摼涓殑鑺傜偣鏃?
        /// - 闇€瑕佽褰曟簮閾綢D銆佹簮鑺傜偣ID鍜岀洰鏍囪妭鐐笽D
        /// - 鐢ㄤ簬鎵ц鏃剁殑鍚屾鍜岄『搴忔帶鍒?
        /// </remarks>
        private void AnalyzeChainDependencies(
            ExecutionChain chain,
            HashSet<string> allVisitedNodes,
            List<ExecutionChain> existingChains)
        {
            foreach (var nodeId in chain.NodeIds)
            {
                var parentIds = Connections
                    .Where(kvp => kvp.Value.Contains(nodeId))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var parentId in parentIds)
                {
                    // 濡傛灉鐖惰妭鐐逛笉鍦ㄦ湰閾句腑锛屽垯鍒涘缓璺ㄩ摼渚濊禆
                    if (!chain.NodeIds.Contains(parentId))
                    {
                        // 鏌ユ壘鐖惰妭鐐规墍鍦ㄧ殑婧愰摼
                        var sourceChain = existingChains.FirstOrDefault(c => c.NodeIds.Contains(parentId));
                        var sourceChainId = sourceChain?.ChainId ?? "unknown";

                        chain.Dependencies.Add(new ChainDependency
                        {
                            SourceChainId = sourceChainId,
                            SourceNodeId = parentId,
                            TargetNodeId = nodeId
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 鍩轰簬鎵ц閾剧殑骞惰鎵ц鍒嗙粍锛堟敼杩涚増 - 浣跨敤灞傜骇鍒嗙粍鏈€澶у寲骞惰搴︼級
        /// </summary>
        /// <returns>骞惰鎵ц缁勫垪琛紝姣忎釜缁勫寘鍚彲骞惰鎵ц鐨勮妭鐐笽D</returns>
        /// <remarks>
        /// 绠楁硶鐗圭偣锛?
        /// 1. 璇嗗埆鎵€鏈夋墽琛岄摼锛堢嫭绔嬬殑鎵ц鍗曞厓锛?
        /// 2. 涓烘瘡涓墽琛岄摼鐢熸垚灞傜骇鍒嗙粍锛圔FS灞傜骇鎰熺煡锛?
        /// 3. 灏嗕笉鍚屾墽琛岄摼涓悓涓€灞傜骇鐨勮妭鐐瑰悎骞跺埌鍚屼竴鎵ц缁?
        /// 4. 纭繚涓嶅悓鎵ц閾剧殑鑺傜偣涓嶄細琚敊璇湴娣峰悎鍒板悓涓€缁?
        /// 
        /// 浼樺娍锛?
        /// - 鏈€澶у寲骞惰搴︼細鍚屼竴灞傜骇鐨勬墍鏈夎妭鐐瑰彲浠ュ苟琛屾墽琛?
        /// - 閬垮厤渚濊禆鍐茬獊锛氶€氳繃灞傜骇鍒嗙粍淇濊瘉鎵ц椤哄簭姝ｇ‘
        /// - 鏀寔璺ㄩ摼鍚屾锛氫笉鍚屾墽琛岄摼鐨勫悓涓€灞傜骇鑺傜偣鍙互鍚屾鎵ц
        /// </remarks>
        public List<List<string>> GetParallelExecutionGroupsByChains()
        {
            var groups = new List<List<string>>();

            // 姝ラ1锛氳瘑鍒墍鏈夋墽琛岄摼
            var chains = GetAutoDetectExecutionChains();
            Logger?.LogInfo($"[ParallelGroups] 璇嗗埆鍒?{chains.Count} 鏉℃墽琛岄摼");

            // 姝ラ2锛氫负姣忎釜鎵ц閾剧敓鎴愬眰绾у垎缁勶紙鑰岄潪绾挎€ф帓搴忥級
            var chainExecutionLevels = new Dictionary<string, List<List<string>>>();

            foreach (var chain in chains)
            {
                var chainNodes = new HashSet<string>(chain.NodeIds);
                var levels = GetExecutionLevelsForNodes(chainNodes);
                chainExecutionLevels[chain.ChainId] = levels;

                Logger?.LogInfo($"[ParallelGroups] 鎵ц閾?{chain.ChainId} ({GetNodeDisplayName(chain.StartNodeId)}):");
                for (int i = 0; i < levels.Count; i++)
                {
                    Logger?.LogInfo($"    灞傜骇{i + 1}: [{FormatNodeListDisplay(levels[i])}]");
                }
            }

            // 姝ラ3锛氳法閾惧苟琛屽悎骞?- 灏嗕笉鍚屾墽琛岄摼涓悓涓€灞傜骇鐨勮妭鐐瑰悎骞?
            int maxLevel = chainExecutionLevels.Values.Max(l => l.Count);

            for (int level = 0; level < maxLevel; level++)
            {
                var currentGroup = new List<string>();

                foreach (var chainId in chainExecutionLevels.Keys)
                {
                    var levels = chainExecutionLevels[chainId];
                    if (level < levels.Count)
                    {
                        // 灏嗚閾惧湪褰撳墠灞傜骇鐨勬墍鏈夎妭鐐瑰姞鍏ユ墽琛岀粍
                        currentGroup.AddRange(levels[level]);
                    }
                }

                if (currentGroup.Count > 0)
                {
                    groups.Add(currentGroup);
                    Logger?.LogInfo($"[ParallelGroups] 鎵ц缁剓level + 1}: [{FormatNodeListDisplay(currentGroup)}]");
                }
            }

            Logger?.LogInfo($"[ParallelGroups] 鍏辩敓鎴?{groups.Count} 涓苟琛屾墽琛岀粍锛堜紭鍖栧悗锛?);
            return groups;
        }

        /// <summary>
        /// 鑾峰彇鎸囧畾鑺傜偣闆嗗悎鐨勬墽琛岄『搴忥紙浣跨敤Kahn绠楁硶杩涜鎷撴墤鎺掑簭锛?
        /// </summary>
        /// <param name="nodeIds">鑺傜偣ID闆嗗悎</param>
        /// <returns>鎸夋嫇鎵戞帓搴忕殑鑺傜偣ID鍒楄〃</returns>
        /// <remarks>
        /// 涓嶨etExecutionOrder鐨勫尯鍒細
        /// - 浠呰€冭檻鎸囧畾闆嗗悎鍐呯殑鑺傜偣鍜岃繛鎺?
        /// - 蹇界暐闆嗗悎澶栬妭鐐圭殑渚濊禆鍏崇郴
        /// - 鐢ㄤ簬瀛愬浘鎴栭儴鍒嗚妭鐐圭殑鎵ц鎺掑簭
        /// </remarks>
        private List<string> GetExecutionOrderForNodes(HashSet<string> nodeIds)
        {
            var order = new List<string>();
            var inDegree = CalculateInDegreesForNodes(nodeIds);

            // 灏嗗叆搴︿负0鐨勮妭鐐瑰姞鍏ラ槦鍒?
            var queue = new Queue<string>();
            foreach (var nodeId in nodeIds)
            {
                if (inDegree[nodeId] == 0)
                {
                    queue.Enqueue(nodeId);
                }
            }

            // 鎷撴墤鎺掑簭澶勭悊
            var processedCount = 0;
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                order.Add(nodeId);
                processedCount++;

                // 鍑忓皯鍚庣户鑺傜偣鐨勫叆搴︼紙浠呰€冭檻闆嗗悎鍐呯殑鑺傜偣锛?
                if (Connections.ContainsKey(nodeId))
                {
                    foreach (var dependentId in Connections[nodeId])
                    {
                        if (nodeIds.Contains(dependentId))
                        {
                            inDegree[dependentId]--;
                            if (inDegree[dependentId] == 0)
                            {
                                queue.Enqueue(dependentId);
                            }
                        }
                    }
                }
            }

            if (processedCount != nodeIds.Count)
            {
                Logger?.LogWarning($"鎷撴墤鎺掑簭妫€娴嬪埌寰幆渚濊禆锛屽凡澶勭悊 {processedCount}/{nodeIds.Count} 涓妭鐐?);
            }

            return order;
        }

        /// <summary>
        /// 鑾峰彇鎸囧畾鑺傜偣闆嗗悎鐨勬墽琛屽眰绾э紙BFS灞傜骇鎰熺煡鍒嗙粍锛?
        /// </summary>
        /// <param name="nodeIds">鑺傜偣ID闆嗗悎</param>
        /// <returns>灞傜骇鍒楄〃锛屾瘡涓眰绾у寘鍚彲骞惰鎵ц鐨勮妭鐐笽D</returns>
        /// <remarks>
        /// 绠楁硶璇存槑锛?
        /// 1. 璁＄畻鑺傜偣闆嗗悎鍐呯殑鍏ュ害锛堜粎鑰冭檻闆嗗悎鍐呯殑杩炴帴锛?
        /// 2. BFS灞傜骇閬嶅巻锛屽皢鍚屼竴灞傜骇鐨勮妭鐐瑰垎缁?
        /// 3. 姣忓眰鍖呭惈鍏ュ害涓?涓旀湭澶勭悊鐨勮妭鐐?
        /// 4. 澶勭悊鍚庡噺灏戝悗缁ц妭鐐圭殑鍏ュ害锛岃繘鍏ヤ笅涓€灞?
        /// 
        /// 浼樺娍锛?
        /// - 鏈€澶у寲骞惰搴︼細鍚屼竴灞傜骇鐨勬墍鏈夎妭鐐瑰彲浠ュ苟琛屾墽琛?
        /// - 淇濊瘉椤哄簭姝ｇ‘锛氶€氳繃灞傜骇鍒嗙粍纭繚渚濊禆鍏崇郴
        /// - 鏀寔閮ㄥ垎鎵ц锛氫粎澶勭悊鎸囧畾闆嗗悎鍐呯殑鑺傜偣
        /// </remarks>
        private List<List<string>> GetExecutionLevelsForNodes(HashSet<string> nodeIds)
        {
            var levels = new List<List<string>>();
            var inDegree = CalculateInDegreesForNodes(nodeIds);
            var remaining = new HashSet<string>(nodeIds);

            // BFS灞傜骇閬嶅巻锛屽皢鍚屼竴灞傜骇鐨勮妭鐐瑰垎缁?
            while (remaining.Count > 0)
            {
                var currentLevel = new List<string>();

                // 鎵惧嚭褰撳墠鎵€鏈夊叆搴︿负0鐨勮妭鐐癸紙鍙苟琛屾墽琛岋級
                foreach (var nodeId in remaining.ToList())
                {
                    if (inDegree[nodeId] == 0)
                    {
                        currentLevel.Add(nodeId);
                    }
                }

                // 娌℃湁鍙墽琛岀殑鑺傜偣锛屽彲鑳芥槸寰幆渚濊禆
                if (currentLevel.Count == 0)
                {
                    Logger?.LogWarning($"鏃犳硶澶勭悊鍓╀綑鑺傜偣锛屽彲鑳藉瓨鍦ㄥ惊鐜緷璧? [{FormatNodeListDisplay(remaining)}]");
                    break;
                }

                levels.Add(currentLevel);

                // 浠庡墿浣欒妭鐐逛腑绉婚櫎宸插鐞嗙殑鑺傜偣
                foreach (var nodeId in currentLevel)
                {
                    remaining.Remove(nodeId);
                }

                // 鍑忓皯杩欎簺鑺傜偣鐨勫悗缁ц妭鐐圭殑鍏ュ害
                ProcessLevelSuccessors(currentLevel, nodeIds, inDegree);
            }

            Logger?.LogInfo($"灞傜骇鍒嗙粍瀹屾垚锛屽叡{levels.Count}涓眰绾?);
            for (int i = 0; i < levels.Count; i++)
            {
                Logger?.LogInfo($"  灞傜骇{i + 1}: [{FormatNodeListDisplay(levels[i])}]");
            }

            return levels;
        }

        /// <summary>
        /// 璁＄畻鎸囧畾鑺傜偣闆嗗悎涓瘡涓妭鐐圭殑鍏ュ害
        /// </summary>
        /// <param name="nodeIds">鑺傜偣ID闆嗗悎</param>
        /// <returns>鑺傜偣鍏ュ害瀛楀吀</returns>
        private Dictionary<string, int> CalculateInDegreesForNodes(HashSet<string> nodeIds)
        {
            var inDegree = new Dictionary<string, int>();

            // 鍒濆鍖栨墍鏈夎妭鐐圭殑鍏ュ害涓?
            foreach (var nodeId in nodeIds)
            {
                inDegree[nodeId] = 0;
            }

            // 璁＄畻闆嗗悎鍐呰繛鎺ョ殑鍏ュ害
            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    if (nodeIds.Contains(targetId) && nodeIds.Contains(connection.Key))
                    {
                        inDegree[targetId]++;
                    }
                }
            }

            return inDegree;
        }

        /// <summary>
        /// 澶勭悊褰撳墠灞傜骇鐨勫悗缁ц妭鐐癸紝鍑忓皯瀹冧滑鐨勫叆搴?
        /// </summary>
        /// <param name="currentLevel">褰撳墠灞傜骇鐨勮妭鐐瑰垪琛?/param>
        /// <param name="nodeIds">鑺傜偣ID闆嗗悎锛堢敤浜庤繃婊わ級</param>
        /// <param name="inDegree">鑺傜偣鍏ュ害瀛楀吀</param>
        private void ProcessLevelSuccessors(
            List<string> currentLevel,
            HashSet<string> nodeIds,
            Dictionary<string, int> inDegree)
        {
            foreach (var nodeId in currentLevel)
            {
                if (Connections.ContainsKey(nodeId))
                {
                    foreach (var dependentId in Connections[nodeId])
                    {
                        if (nodeIds.Contains(dependentId) && inDegree.ContainsKey(dependentId))
                        {
                            inDegree[dependentId]--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 鑾峰彇鑺傜偣鐨勫畬鏁存樉绀哄悕绉帮紙鏍煎紡锛氳妭鐐瑰悕绉?ID: xxx)锛?
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>鑺傜偣鐨勫畬鏁存樉绀哄悕绉帮紝濡傛灉鑺傜偣涓嶅瓨鍦ㄥ垯杩斿洖ID鏈韩</returns>
        private string GetNodeDisplayName(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                return $"{node.Name}(ID: {nodeId})";
            }
            return nodeId;
        }

        /// <summary>
        /// 涓鸿妭鐐瑰噯澶囪緭鍏ユ暟鎹?
        /// 濡傛灉鑺傜偣鏈夌埗鑺傜偣锛屽垯浣跨敤鐖惰妭鐐圭殑杈撳嚭浣滀负杈撳叆锛涘惁鍒欎娇鐢ㄥ師濮嬭緭鍏?
        /// </summary>
        /// <param name="node">鐩爣鑺傜偣</param>
        /// <param name="defaultInput">榛樿杈撳叆鏁版嵁</param>
        /// <param name="nodeResults">鑺傜偣鎵ц缁撴灉鏄犲皠琛?/param>
        /// <returns>鑺傜偣鐨勮緭鍏ユ暟鎹?/returns>
        private Mat PrepareNodeInput(
            WorkflowNode node,
            Mat defaultInput,
            Dictionary<string, Mat> nodeResults)
        {
            var input = defaultInput;

            if (Connections.Any(kvp => kvp.Value.Contains(node.Id)))
            {
                // 鑾峰彇鐖惰妭鐐圭殑杈撳嚭浣滀负杈撳叆
                var parentIds = Connections
                    .Where(kvp => kvp.Value.Contains(node.Id))
                    .Select(kvp => kvp.Key);

                var parentResults = parentIds
                    .Where(id => nodeResults.ContainsKey(id))
                    .Select(id => nodeResults[id])
                    .ToList();

                if (parentResults.Any())
                {
                    input = parentResults.First();
                }
            }

            return input;
        }

        /// <summary>
        /// 鏍煎紡鍖栬妭鐐笽D鍒楄〃涓烘樉绀哄悕绉板垪琛?
        /// </summary>
        /// <param name="nodeIds">鑺傜偣ID鍒楄〃</param>
        /// <returns>鏍煎紡鍖栧悗鐨勮妭鐐规樉绀哄悕绉板垪琛ㄥ瓧绗︿覆</returns>
        private string FormatNodeListDisplay(IEnumerable<string> nodeIds)
        {
            return string.Join(", ", nodeIds.Select(id => GetNodeDisplayName(id)));
        }

        /// <summary>
        /// 璁＄畻鎵€鏈夎妭鐐圭殑鍏ュ害
        /// </summary>
        /// <returns>鑺傜偣ID鍒板叆搴︾殑鏄犲皠瀛楀吀</returns>
        private Dictionary<string, int> CalculateNodeInDegrees()
        {
            var inDegree = new Dictionary<string, int>();

            // 鍒濆鍖栨墍鏈夎妭鐐圭殑鍏ュ害涓?
            foreach (var node in Nodes)
            {
                inDegree[node.Id] = 0;
            }

            // 閬嶅巻鎵€鏈夎繛鎺ワ紝璁＄畻姣忎釜鐩爣鑺傜偣鐨勫叆搴?
            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    inDegree[targetId]++;
                }
            }

            return inDegree;
        }

        /// <summary>
        /// 璁＄畻鎸囧畾鑺傜偣缁勭殑鎵€鏈変笅娓歌妭鐐?
        /// </summary>
        /// <param name="nodes">鑺傜偣鍒楄〃</param>
        /// <returns>涓嬫父鑺傜偣ID闆嗗悎</returns>
        private HashSet<string> CalculateDownstreamNodes(List<WorkflowNode> nodes)
        {
            var downstream = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (Connections.ContainsKey(node.Id))
                {
                    downstream.UnionWith(Connections[node.Id]);
                }
            }

            return downstream;
        }

        /// <summary>
        /// 鏍规嵁涓嬫父鑺傜偣閲嶅彔鎯呭喌瀵瑰叆鍙ｈ妭鐐硅繘琛屽垎缁?
        /// 灏嗗叡浜笅娓歌妭鐐圭殑鍏ュ彛鑺傜偣鍚堝苟鍒板悓涓€缁勶紝浠ヤ究鏋勫缓鍗曚釜鎵ц閾?
        /// </summary>
        /// <param name="entryNodes">鍏ュ彛鑺傜偣鍒楄〃</param>
        /// <returns>鍒嗙粍鍚庣殑鍏ュ彛鑺傜偣缁勫垪琛?/returns>
        private List<List<WorkflowNode>> GroupEntryNodesByDownstream(List<WorkflowNode> entryNodes)
        {
            var entryGroups = new List<List<WorkflowNode>>();

            foreach (var entryNode in entryNodes)
            {
                var merged = false;
                var currentDownstream = CalculateDownstreamNodes(new List<WorkflowNode> { entryNode });

                for (int groupIndex = 0; groupIndex < entryGroups.Count; groupIndex++)
                {
                    var group = entryGroups[groupIndex];
                    var groupDownstream = CalculateDownstreamNodes(group);

                    // 濡傛灉涓嬫父鑺傜偣鏈変氦闆嗭紝鍒欏悎骞跺埌鍚屼竴缁?
                    if (groupDownstream.Overlaps(currentDownstream))
                    {
                        group.Add(entryNode);
                        merged = true;
                        Logger?.LogInfo($"[AutoDetect] 鍚堝苟鍏ュ彛鑺傜偣: {entryNode.Name} -> 缁剓groupIndex}");
                        break;
                    }
                }

                // 濡傛灉娌℃湁鎵惧埌閲嶅彔鐨勭粍锛屽垯鍒涘缓鏂扮粍
                if (!merged)
                {
                    entryGroups.Add(new List<WorkflowNode> { entryNode });
                }
            }

            Logger?.LogInfo($"[AutoDetect] 灏?{entryNodes.Count} 涓叆鍙ｈ妭鐐瑰悎骞朵负 {entryGroups.Count} 缁?);
            return entryGroups;
        }

        /// <summary>
        /// 涓烘寚瀹氱殑鍏ュ彛鑺傜偣缁勫垱寤烘墽琛岄摼
        /// </summary>
        /// <param name="entryGroup">鍏ュ彛鑺傜偣缁?/param>
        /// <param name="chainIndex">鎵ц閾剧储寮?/param>
        /// <param name="allVisitedNodes">宸茶闂殑鑺傜偣闆嗗悎锛堢敤浜庢爣璁板凡澶勭悊鐨勮妭鐐癸級</param>
        /// <param name="existingChains">宸插瓨鍦ㄧ殑鎵ц閾惧垪琛紙鐢ㄤ簬鍒嗘瀽璺ㄩ摼渚濊禆锛?/param>
        /// <returns>鍒涘缓鐨勬墽琛岄摼</returns>
        private ExecutionChain CreateExecutionChain(
            List<WorkflowNode> entryGroup,
            int chainIndex,
            HashSet<string> allVisitedNodes,
            List<ExecutionChain> existingChains)
        {
            var chain = new ExecutionChain
            {
                ChainId = $"chain_{chainIndex}",
                StartNodeId = entryGroup.First().Id,
                NodeIds = new List<string>(),
                Dependencies = new List<ChainDependency>()
            };

            // 閫掑綊鏀堕泦璇ョ粍鎵€鏈夊叆鍙ｈ妭鐐逛笅娓哥殑鎵€鏈夎妭鐐?
            foreach (var entryNode in entryGroup)
            {
                CollectExecutionChain(entryNode.Id, chain.NodeIds, allVisitedNodes);
            }

            // 鍒嗘瀽璺ㄩ摼渚濊禆鍏崇郴
            AnalyzeChainDependencies(chain, allVisitedNodes, existingChains);

            return chain;
        }

        /// <summary>
        /// 涓烘湭璁块棶鐨勮妭鐐癸紙瀛ゅ矝鑺傜偣锛夊垱寤虹嫭绔嬬殑鎵ц閾?
        /// </summary>
        /// <param name="allVisitedNodes">宸茶闂殑鑺傜偣闆嗗悎</param>
        /// <param name="chainCounter">鎵ц閾捐鏁板櫒锛堝紩鐢ㄧ被鍨嬶紝鐢ㄤ簬閫掑绱㈠紩锛?/param>
        /// <param name="chains">鎵ц閾惧垪琛紙鐢ㄤ簬娣诲姞鏂扮殑瀛ゅ矝閾撅級</param>
        private void CreateIsolatedChains(
            HashSet<string> allVisitedNodes,
            ref int chainCounter,
            List<ExecutionChain> chains)
        {
            var unvisitedNodes = Nodes
                .Where(n => !allVisitedNodes.Contains(n.Id) && n.IsEnabled)
                .ToList();

            foreach (var isolatedNode in unvisitedNodes)
            {
                var chain = new ExecutionChain
                {
                    ChainId = $"chain_{chainCounter}",
                    StartNodeId = isolatedNode.Id,
                    NodeIds = new List<string> { isolatedNode.Id },
                    Dependencies = new List<ChainDependency>()
                };

                chains.Add(chain);
                chainCounter++;

                Logger?.LogInfo($"[AutoDetect] 鍒涘缓瀛ゅ矝閾綶{chainCounter}]: {isolatedNode.Name}");
            }
        }
    }

    /// <summary>
    /// 鎵ц缁勭姸鎬?
    /// </summary>
    public enum ExecutionGroupStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// 鎵ц缁?
    /// </summary>
    public class ExecutionGroup
    {
        public int GroupNumber { get; set; }
        public List<string> NodeIds { get; set; } = new List<string>();
        public ExecutionGroupStatus Status { get; set; }
    }

    /// <summary>
    /// 鎵ц璁″垝
    /// </summary>
    public class ExecutionPlan
    {
        public List<ExecutionGroup> Groups { get; set; } = new List<ExecutionGroup>();
        private DateTime _startTime;
        private DateTime _endTime;

        public void Start()
        {
            _startTime = DateTime.Now;
        }

        public void Complete()
        {
            _endTime = DateTime.Now;
        }

        public string GetReport()
        {
            var duration = (_endTime - _startTime).TotalMilliseconds;
            var report = $"Execution completed in {duration:F2}ms\n";
            report += $"Groups: {Groups.Count}\n";

            foreach (var group in Groups)
            {
                report += $"  Group {group.GroupNumber}: {group.NodeIds.Count} nodes - {group.Status}\n";
            }

            return report;
        }
    }
}
