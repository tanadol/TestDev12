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
    $username = isset($_POST['username']) ? trim($_POST['username']) : '';
    $password = isset($_POST['password']) ? trim($_POST['password']) : '';

    if (empty($username) || empty($password)) {
        echo json_encode(["status" => "error", "message" => "nousernamorpassword"]);
        exit;
    }

    // Verify user credentials and fetch data from both user and user_data table
    $stmt = $conn->prepare("
        SELECT u.user_id, u.username, u.password, ud.diamond, ud.heart
        FROM user u
        INNER JOIN user_data ud ON u.user_id = ud.user_id
        WHERE u.username = :username
    ");
    $stmt->execute(['username' => $username]);

    if ($user = $stmt->fetch(PDO::FETCH_ASSOC)) {
        // Verify password using password_verify() for BCRYPT
        if (password_verify($password, $user['password'])) {
            // Record the login history in the loginhistory table
            $stmt = $conn->prepare("INSERT INTO loginhistory (user_id, logintime) VALUES (:user_id, NOW())");
            $stmt->execute(['user_id' => $user['user_id']]);

            echo json_encode([
                "status" => "success",
                "message" => "loginsuccess",
                "user_id" => $user['user_id'],
                "username" => $user['username'],  // Include the username in the response
                "diamond" => $user['diamond'],
                "heart" => $user['heart']
            ]);
        } else {
            echo json_encode(["status" => "error", "message" => "loginfailed"]);
        }
    } else {
        echo json_encode(["status" => "error", "message" => "loginfailed"]);
    }
} else {
    echo json_encode(["status" => "error", "message" => "Invalid request method."]);
}

?>
