<?php

header('Content-Type: application/json');

// Include database configuration
require_once 'config.php';

try {
    $conn = new PDO("mysql:host=$servername;dbname=$dbname", $dbusername, $dbpassword);
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    echo json_encode(["status" => "error", "message" => "Database connection failed: " . $e->getMessage()]);
    exit;
}

// Check if data is received via POST
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    // Get user_id from POST
    $user_id = isset($_POST['user_id']) ? (int)$_POST['user_id'] : 0;

    if ($user_id <= 0) {
        echo json_encode(["status" => "error", "message" => "invaliduser"]);
        exit;
    }

    // Fetch user data from user and user_data table based on user_id
    $stmt = $conn->prepare("
        SELECT u.user_id, u.username, ud.diamond, ud.heart
        FROM user u
        INNER JOIN user_data ud ON u.user_id = ud.user_id
        WHERE u.user_id = :user_id
    ");
    $stmt->execute(['user_id' => $user_id]);

    if ($user = $stmt->fetch(PDO::FETCH_ASSOC)) {
        // Return user data
        echo json_encode([
            "status" => "success",
            "user_id" => $user['user_id'],
            "username" => $user['username'],
            "diamond" => $user['diamond'],
            "heart" => $user['heart']
        ]);
    } else {
        echo json_encode(["status" => "error", "message" => "loginfailed"]);
    }
} else {
    echo json_encode(["status" => "error", "message" => "Invalid request method."]);
}